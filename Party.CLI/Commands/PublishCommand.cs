using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Serializers;

namespace Party.CLI.Commands
{
    public class PublishCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("publish", "Prepares files for publishing (using a folder, a list of files or a list of urls)");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("input", null) { Arity = ArgumentArity.OneOrMore });
            command.AddOption(new Option("--package-name", "The name of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--package-version", "The version of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--package-author", "The author name of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--registry", "Path the the index.json file of your locally cloned registry") { Argument = new Argument<FileInfo>().ExistingOnly() });
            command.AddOption(new Option("--saves", "Specify a custom saves folder, e.g. when the script is not in the Virt-A-Mate folder") { Argument = new Argument<DirectoryInfo>().ExistingOnly() });
            command.AddOption(new Option("--quiet", "Just print the hash and metadata, no questions asked"));

            command.Handler = CommandHandler.Create<PublishArguments>(async args =>
            {
                await new PublishCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class PublishArguments : CommonArguments
        {
            public string[] Input { get; set; }
            public string PackageName { get; set; }
            public string PackageVersion { get; set; }
            public string PackageAuthor { get; set; }
            public FileInfo Registry { get; set; }
            public DirectoryInfo Saves { get; set; }
            public bool Quiet { get; set; }
        }

        public PublishCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(PublishArguments args)
        {
            Controller.HealthCheck();

            Registry registry;
            if (args.Registry != null)
            {
                if (args.Registry.Name != "index.json")
                    throw new UserInputException("Please specify the path to your locally cloned index.json file");

                registry = await Controller.GetRegistryAsync(args.Registry.FullName);
            }
            else
            {
                registry = await Controller.GetRegistryAsync();
            }

            var name = args.PackageName ?? (args.Quiet ? "unnamed" : Renderer.Ask("Package Name: ", false, RegistryScript.ValidNameRegex, "my-package"));

            var script = registry.GetOrCreateScript(name);
            var version = script.CreateVersion();

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

            foreach (var pathOrUrl in pathOrUrls)
            {
                version.Files.AddRange(pathOrUrl.StartsWith("http") && Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var url)
                     ? await Controller.BuildRegistryFilesFromUrlAsync(registry, url).ConfigureAwait(false)
                     : await Controller.BuildRegistryFilesFromPathAsync(registry, Path.GetFullPath(pathOrUrl), args.Saves).ConfigureAwait(false));
            }

            registry.AssertNoDuplicates(version);

            if (script == null || version == null || script.Versions == null) throw new NullReferenceException($"Error in {nameof(Controller.BuildRegistryFilesFromPathAsync)}: Null values were returned.");

            var isNew = script.Versions.Count == 1;

            // TODO: Validate all fields
            if (!args.Quiet && !isNew && string.IsNullOrEmpty(args.PackageVersion))
            {
                Renderer.WriteLine($"This package already exists (by {script.Author ?? "Unknown User"}), a new version will be added to it.");

                Renderer.WriteLine($"Latest {Math.Min(5, script.Versions.Count - 1)} versions:");
                foreach (var existingVersion in script.Versions.Where(v => !ReferenceEquals(v, version)).Take(5))
                {
                    Renderer.WriteLine($"- {existingVersion.Version}");
                }
            }

            version.Created = DateTimeOffset.Now;
            version.Version = args.PackageVersion ?? (args.Quiet ? "1.0.0" : Renderer.Ask("Package version: ", true, RegistryScriptVersion.ValidVersionNameRegex, "1.0.0"));

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

                script.Author = author;

                if (!args.Quiet)
                {
                    script.Description = Renderer.Ask("Description: ");
                    script.Tags = (Renderer.Ask("Tags (comma-separated list): ") ?? "").Split(',').Select(x => x.Trim()).Where(x => x != "").ToList();
                    script.Homepage = Renderer.Ask("Package Homepage URL: ");
                    script.Repository = Renderer.Ask("Package Repository URL: ");
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
                        file.Url = Renderer.Ask($"{file.Filename} URL ({fileUrl}): ") ?? fileUrl;
                    }
                    else
                    {
                        file.Url = Renderer.Ask($"{file.Filename} URL: ", true);
                        if (file.Url.EndsWith("/" + file.Filename.Replace(" ", "%20")))
                        {
                            baseUrl = file.Url.Substring(0, file.Url.LastIndexOf("/") + 1);
                        }
                    }
                }
            }

            var serializer = new RegistrySerializer();
            if (args.Registry != null)
            {
                Controller.SaveToFile(serializer.Serialize(registry), args.Registry.FullName, false);
                Renderer.WriteLine($"JSON written to {args.Registry.FullName}");
            }
            else
            {
                Renderer.WriteLine("JSON Template:");
                Renderer.WriteLine(serializer.Serialize(script));
            }
        }
    }
}
