using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;

namespace Party.CLI.Commands
{
    public class ShowCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("show", "Show information about a package");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string package, bool warnings) =>
            {
                await new ShowCommand(renderer, config, saves, controller).ExecuteAsync(package, warnings);
            });
            return command;
        }

        public ShowCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string package, bool warnings)
        {
            var (saves, registry) = await GetSavesAndRegistryAsync();

            var registryPackage = registry.Scripts?.FirstOrDefault(p => p.Name.Equals(package, StringComparison.InvariantCultureIgnoreCase));

            if (registryPackage == null)
            {
                throw new UserInputException($"Could not find package {package}");
            }

            var registryVersion = registryPackage.GetLatestVersion();

            if (registryVersion?.Files == null)
            {
                throw new RegistryException("Package does not have any versions");
            }

            PrintWarnings(warnings, saves.Errors);

            Renderer.WriteLine($"Package {registryPackage.Name}, by {registryPackage.Author?.Name ?? "Anonymous"}");
            Renderer.WriteLine($"Last version v{registryVersion.Version}, published {registryVersion.Created.ToLocalTime().ToString("D")}");
            if (registryPackage.Description != null)
            {
                Renderer.WriteLine($"Description: {registryPackage.Description}");
            }
            if (registryPackage.Tags != null)
            {
                Renderer.WriteLine($"Tags: {string.Join(", ", registryPackage.Tags)}");
            }
            if (registryPackage.Repository != null)
            {
                Renderer.WriteLine($"Repository: {registryPackage.Repository}");
            }
            if (registryPackage.Homepage != null)
            {
                Renderer.WriteLine($"Homepage: {registryPackage.Homepage}");
            }
            Renderer.WriteLine("Files:");
            foreach (var file in registryVersion.Files)
            {
                Renderer.WriteLine($"- {file.Filename}");
            }
        }
    }
}
