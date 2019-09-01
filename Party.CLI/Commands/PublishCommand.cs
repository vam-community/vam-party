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
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("publish", "Prepares files for publishing");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("path-or-url", null) { Arity = ArgumentArity.OneOrMore });
            command.AddOption(new Option("--package-name", "The name of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--package-version", "The version of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--registry", "Path the the index.json file of your locally cloned registry") { Argument = new Argument<FileInfo>().ExistingOnly() });
            // TODO: Add the different fields too (author, name, etc.)

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string[] pathOrUrl, string packageName, string packageVersion, FileInfo registry) =>
            {
                await new PublishCommand(renderer, config, saves, controller).ExecuteAsync(pathOrUrl, packageName, packageVersion, registry);
            });
            return command;
        }

        public PublishCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string[] pathOrUrls, string packageName, string packageVersion, FileInfo registryJson)
        {
            Registry registry;
            if (registryJson != null)
            {
                if (registryJson.Name != "index.json")
                    throw new UserInputException("Please specify the path to your locally cloned index.json file");

                registry = await Controller.GetRegistryAsync(registryJson.FullName);
            }
            else
            {
                registry = await Controller.GetRegistryAsync();
            }

            var name = packageName ?? Renderer.Ask("Package Name: ", false, RegistryScript.ValidNameRegex, "my-package");

            var script = registry.GetOrCreateScript(name);
            var version = script.CreateVersion();

            foreach (var pathOrUrl in pathOrUrls)
            {
                version.Files.AddRange(Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var url)
                     ? await Controller.BuildRegistryFilesFromUrlAsync(registry, name, url).ConfigureAwait(false)
                     : await Controller.BuildRegistryFilesFromPathAsync(registry, name, Path.GetFullPath(pathOrUrl)).ConfigureAwait(false)
                );
            }

            registry.AssertNoDuplicates(version);

            if (script == null || version == null || script.Versions == null) throw new NullReferenceException($"Error in {nameof(Controller.BuildRegistryFilesFromPathAsync)}: Null values were returned.");

            var isNew = script.Versions.Count == 1;

            // TODO: Validate all fields
            if (!isNew && string.IsNullOrEmpty(packageVersion))
            {
                Renderer.WriteLine($"This package already exists (by {script.Author ?? "Unknown User"}), a new version will be added to it.");

                Renderer.WriteLine($"Latest {Math.Min(5, script.Versions.Count - 1)} versions:");
                foreach (var existingVersion in script.Versions.Where(v => !ReferenceEquals(v, version)).Take(5))
                {
                    Renderer.WriteLine($"- {existingVersion.Version}");
                }
            }

            version.Created = DateTimeOffset.Now;
            version.Version = packageVersion ?? Renderer.Ask("Package Version: ", true, RegistryScriptVersion.ValidVersionNameRegex, "1.0.0");
            version.Notes = Renderer.Ask("Release notes: ");

            if (isNew)
            {
                Renderer.WriteLine("Looks like a new package in the registry! Please provide some information about this new package, or press CTRL+C if you want to abort.");

                var author = Renderer.Ask("Author Name: ", true);
                if (!registry.Authors.Any(a => a.Name.Equals(author, StringComparison.InvariantCultureIgnoreCase)))
                {
                    registry.Authors.Add(new RegistryAuthor
                    {
                        Name = author,
                        Github = Renderer.Ask($"GitHub Profile URL: "),
                        Reddit = Renderer.Ask($"Reddit Profile URL: ")
                    });
                }

                script.Author = author;
                script.Description = Renderer.Ask("Description: ");
                script.Tags = (Renderer.Ask("Tags (comma-separated list): ") ?? "").Split(',').Select(x => x.Trim()).Where(x => x != "").ToList();
                script.Homepage = Renderer.Ask("Package Homepage URL: ");
                script.Repository = Renderer.Ask("Package Repository URL: ");
            }

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

            var serializer = new RegistrySerializer();
            if (registryJson != null)
            {
                Controller.SaveToFile(serializer.Serialize(registry), registryJson.FullName, false);
                Renderer.WriteLine($"JSON written to {registryJson.FullName}");
            }
            else
            {
                Renderer.WriteLine("JSON Template:");
                Renderer.WriteLine(serializer.Serialize(script));
            }
        }
    }
}
