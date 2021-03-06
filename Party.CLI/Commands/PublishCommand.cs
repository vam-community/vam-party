﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models.Registries;
using Party.Shared.Serializers;

namespace Party.CLI.Commands
{
    public class PublishCommand : CommandBase<PublishArguments>
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("publish", "Prepares files for publishing (using a folder, a list of files or a list of urls)");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("input", null) { Arity = ArgumentArity.OneOrMore });
            command.AddOption(new Option("--package-name", "The name of your package") { Argument = new Argument<string>() }.WithAlias("-pn"));
            command.AddOption(new Option("--package-version", "The version of your package") { Argument = new Argument<string>() }.WithAlias("-v"));
            command.AddOption(new Option("--package-author", "The author name of your package") { Argument = new Argument<string>() }.WithAlias("-pa"));
            command.AddOption(new Option("--package-version-download-url", "The url to download this version") { Argument = new Argument<string>() }.WithAlias("-pu"));
            command.AddOption(new Option("--registry", "Path the the index.json file of your locally cloned registry") { Argument = new Argument<FileInfo>().ExistingOnly() }.WithAlias("-r"));
            command.AddOption(new Option("--quiet", "Just print the hash and metadata, no questions asked").WithAlias("-q"));
            command.AddOption(new Option("--format", "Just format the registry, e.g. after manually editing it").WithAlias("-fmt"));

            command.Handler = CommandHandler.Create<PublishArguments>(async args =>
            {
                await new PublishCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        private readonly IRegistrySerializer _serializer = new RegistrySerializer();

        public PublishCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        protected override async Task ExecuteImplAsync(PublishArguments args)
        {
            ValidateArguments(args.Input);
            Controller.HealthCheck();

            Registry registry;
            if (args.Registry != null)
            {
                if (args.Registry.Name != "index.json")
                    throw new UserInputException("Please specify the path to your locally cloned index.json file");

                registry = await Controller.AcquireRegistryAsync(args.Registry.FullName);

                if (args.Format)
                {
                    Controller.SaveRegistry(registry, args.Registry.FullName);
                    return;
                }
            }
            else
            {
                registry = await Controller.AcquireRegistryAsync();

                if (args.Format)
                    throw new UserInputException("Cannot specify --format without --registry");
            }

            var name = args.PackageName ?? (args.Quiet ? "unnamed" : Renderer.Ask("Package Name: ", false, RegistryPackage.ValidNameRegex, "my-package"));

            // TODO: Handle for other types
            var package = registry.GetOrCreatePackage(RegistryPackageType.Scripts, name);
            var version = package.CreateVersion();

            var pathOrUrls = args.Input;
            if (!args.Quiet && (pathOrUrls == null || pathOrUrls.Length == 0))
            {
                Renderer.WriteLine("No files were provided; please enter each file, folder or url. When done, enter an empty line.");
                var fileInputs = new List<string>();
                var empty = false;
                var counter = 1;
                do
                {
                    var line = Renderer.Ask($"File/Folder/Url {counter++}: ");
                    empty = string.IsNullOrWhiteSpace(line);
                    if (!empty)
                        fileInputs.Add(line.Trim());
                }
                while (!empty);
                pathOrUrls = fileInputs.ToArray();
            }

            if (pathOrUrls != null)
            {
                foreach (var pathOrUrl in pathOrUrls)
                {
                    var sanitizedPathOrUrl = pathOrUrl.Trim('"');
                    try
                    {
                        version.Files.AddRange(sanitizedPathOrUrl.StartsWith("http") && Uri.TryCreate(sanitizedPathOrUrl, UriKind.Absolute, out var url)
                             ? await Controller.BuildRegistryFilesFromUrlAsync(registry, url).ConfigureAwait(false)
                             : await Controller.BuildRegistryFilesFromPathAsync(registry, Path.GetFullPath(sanitizedPathOrUrl)).ConfigureAwait(false));
                    }
                    catch (ArgumentException exc)
                    {
                        throw new UserInputException($"Failed to process file '{sanitizedPathOrUrl}': {exc.Message}");
                    }
                }
            }

            registry.AssertNoDuplicates(RegistryPackageType.Scripts, version);

            if (package == null || version == null || package.Versions == null) throw new NullReferenceException($"Error in {nameof(Controller.BuildRegistryFilesFromPathAsync)}: Null values were returned.");

            var isNew = package.Versions.Count == 1;

            // TODO: Validate all fields
            if (!args.Quiet && !isNew && string.IsNullOrEmpty(args.PackageVersion))
            {
                Renderer.WriteLine($"This package already exists (by {package.Author ?? "Unknown User"}), a new version will be added to it.");

                Renderer.WriteLine($"Latest {Math.Min(5, package.Versions.Count - 1)} versions:");
                foreach (var existingVersion in package.Versions.Where(v => !ReferenceEquals(v, version)).Take(5))
                {
                    Renderer.WriteLine($"- {existingVersion.Version}");
                }
            }

            version.Created = DateTimeOffset.Now;
            version.Version = args.PackageVersion ?? (args.Quiet ? "1.0.0" : Renderer.Ask("Package version: ", true, RegistryPackageVersion.ValidVersionNameRegex, "1.0.0"));
            version.DownloadUrl = args.PackageVersionDownloadUrl ?? (args.Quiet ? null : Renderer.Ask("Package Download Url: "));

            if (!args.Quiet && !isNew)
            {
                version.Notes = Renderer.Ask("Release notes: ");
            }

            if (isNew)
            {
                if (!args.Quiet)
                    Renderer.WriteLine("Looks like a new package in the registry! If this was not what you expected, you might have mistyped the package name; press CTRL+C if you want to abort.");

                var author = args.PackageAuthor ?? (args.Quiet ? "Anonymous" : Renderer.Ask("Author Name: ", true));
                if (!registry.Authors.Any(a => a.Name.Equals(author, StringComparison.InvariantCultureIgnoreCase)))
                {
                    registry.Authors.Add(new RegistryAuthor
                    {
                        Name = author,
                        Github = args.Quiet ? null : Renderer.Ask($"GitHub Profile URL: "),
                        Reddit = args.Quiet ? null : Renderer.Ask($"Reddit Profile URL: ")
                    });
                }

                package.Author = author;

                if (!args.Quiet)
                {
                    package.Description = Renderer.Ask("Description: ");
                    package.Tags = (Renderer.Ask("Tags (comma-separated list): ") ?? "").Split(',').Select(x => x.Trim()).Where(x => x != "").ToList();
                    package.Homepage = Renderer.Ask("Package Homepage URL: ");
                    package.Repository = Renderer.Ask("Package Repository URL: ");
                }
            }

            if (!args.Quiet)
            {
                string baseUrl = null;
                foreach (var file in version.Files)
                {
                    if (!string.IsNullOrEmpty(file.Url)) continue;

                    if (baseUrl != null)
                    {
                        var fileUrl = $"{baseUrl}{file.Filename.Replace(" ", "%20")}";
                        file.Url = Renderer.Ask($"{file.Filename} URL ({fileUrl}): ", false) ?? fileUrl;
                    }
                    else
                    {
                        file.Url = Renderer.Ask($"{file.Filename} URL: ", false);
                        if (file.Url != null && file.Url.EndsWith("/" + file.Filename.Replace(" ", "%20")))
                        {
                            baseUrl = file.Url.Substring(0, file.Url.LastIndexOf("/") + 1);
                        }
                    }
                }
            }

            if (args.Registry != null)
            {
                Controller.SaveRegistry(registry, args.Registry.FullName);
                // NOTE: This is a workaround for incorrect members sorting, not sure why
                Renderer.WriteLine($"JSON written to {args.Registry.FullName}");
            }
            else
            {
                Renderer.WriteLine("JSON Template:");
                Renderer.WriteLine(_serializer.Serialize(package));
            }
        }
    }

    public class PublishArguments : CommonArguments
    {
        public string[] Input { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string PackageAuthor { get; set; }
        public string PackageVersionDownloadUrl { get; set; }
        public FileInfo Registry { get; set; }
        public bool Quiet { get; set; }
        public bool Format { get; set; }
    }
}
