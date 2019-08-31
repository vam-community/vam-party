using System;
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
            command.AddArgument(new Argument<string>("package-path", null));
            command.AddOption(new Option("--package-name", "The name of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--package-version", "The version of your package") { Argument = new Argument<string>() });
            command.AddOption(new Option("--registry", "Path the the index.json file of your locally cloned registry") { Argument = new Argument<FileInfo>().ExistingOnly() });
            // TODO: Add the different fields too (author, name, etc.)

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string packagePath, string packageName, string packageVersion, FileInfo registry) =>
            {
                await new PublishCommand(renderer, config, saves, controller).ExecuteAsync(packagePath, packageName, packageVersion, registry);
            });
            return command;
        }

        public PublishCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string input, string packageName, string packageVersion, FileInfo registryJson)
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

            var (script, version) = await Controller.AddFilesToRegistryAsync(registry, name, Path.GetFullPath(input)).ConfigureAwait(false);

            if (script == null || version == null || script.Versions == null) throw new NullReferenceException($"Error in {nameof(Controller.AddFilesToRegistryAsync)}: Null values were returned.");

            var isNew = script.Versions.Count == 1;

            // TODO: Validate all fields
            if (!isNew)
            {
                Renderer.WriteLine($"This package already exists (by {script.Author?.Name ?? "Anonymous User"}), a new version will be added to it.");

                Renderer.WriteLine($"Latest {Math.Min(5, script.Versions.Count)} versions:");
                foreach (var existingVersion in script.SortedVersions().Take(5))
                {
                    Renderer.WriteLine($"- {existingVersion.Version}");
                }
            }

            version.Version = packageVersion ?? Renderer.Ask("Package Version: ", true, RegistryScriptVersion.ValidVersionNameRegex, "1.0.0");

            if (isNew)
            {
                Renderer.WriteLine("Looks like a new package in the registry! Please provide some information about this new package, or press CTRL+C if you want to abort.");

                var author = new RegistryScriptAuthor
                {
                    Name = Renderer.Ask("Author Name: ", true)
                };
                var existingAuthor = registry.Scripts.Where(s => s.Author != null).Select(s => s.Author).FirstOrDefault(a => a.Name.Equals(author.Name, StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrEmpty(existingAuthor?.Profile))
                    author.Profile = Renderer.Ask($"Author Profile URL ({existingAuthor.Profile}): ") ?? existingAuthor.Profile;
                else
                    author.Profile = Renderer.Ask("Author Profile URL ");
                script.Author = author;

                script.Description = Renderer.Ask("Description: ");
                script.Tags = (Renderer.Ask("Tags (comma-separated list): ")).Split(',').Select(x => x.Trim()).Where(x => x != "").ToList();
                script.Homepage = Renderer.Ask("Package Homepage URL: ");
                script.Repository = Renderer.Ask("Package Repository URL: ");
            }

            string baseUrl = null;
            foreach (var file in version.Files)
            {
                if (baseUrl != null)
                {
                    var url = $"{baseUrl}{file.Filename.Replace(" ", "%20")}";
                    file.Url = Renderer.Ask($"{file.Filename} URL ({url}): ") ?? url;
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
                Controller.SaveToFile(serializer.Serialize(registry), registryJson.FullName);
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
