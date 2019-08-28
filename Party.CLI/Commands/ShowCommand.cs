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

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string package) =>
            {
                await new ShowCommand(renderer, config, saves, controller).ExecuteAsync(package);
            });
            return command;
        }

        public ShowCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string package)
        {
            var registryTask = Controller.GetRegistryAsync();
            var savesTask = Controller.GetSavesAsync();
            await Task.WhenAll();
            var registry = await registryTask;
            var saves = await savesTask;

            var registryPackage = registry.Scripts?.FirstOrDefault(p => p.Name.Equals(package, StringComparison.InvariantCultureIgnoreCase));

            if (registryPackage == null)
            {
                throw new UserInputException($"Could not find package {package}");
            }

            var registryVersion = registryPackage.Versions?.FirstOrDefault();

            if (registryVersion?.Files == null)
            {
                throw new RegistryException("Package does not have any versions");
            }

            PrintWarnings(saves.Errors);

            Renderer.WriteLine($"Package {registryPackage.Name}, by {registryPackage.Author?.Name ?? "Anonymous"}");
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
