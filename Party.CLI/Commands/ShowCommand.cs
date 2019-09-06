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
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("show", "Show information about a package");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));

            command.Handler = CommandHandler.Create<ShowArguments>(async args =>
            {
                await new ShowCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class ShowArguments : CommonArguments
        {
            public string Package { get; set; }
            public bool Warnings { get; set; }
        }

        public ShowCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(ShowArguments args)
        {
            Controller.HealthCheck();

            var (saves, registry) = await GetSavesAndRegistryAsync();

            var registryPackage = registry.Scripts?.FirstOrDefault(p => p.Name.Equals(args.Package, StringComparison.InvariantCultureIgnoreCase));

            if (registryPackage == null)
            {
                throw new UserInputException($"Could not find package {args.Package}");
            }

            var registryVersion = registryPackage.GetLatestVersion();

            if (registryVersion?.Files == null)
            {
                throw new RegistryException("Package does not have any versions");
            }

            PrintWarnings(args.Warnings, saves.Errors);

            Renderer.WriteLine($"Package {registryPackage.Name}");

            Renderer.WriteLine($"Last version v{registryVersion.Version}, published {registryVersion.Created.ToLocalTime().ToString("D")}");

            Renderer.WriteLine("Versions:");
            foreach (var version in registryPackage.Versions)
            {
                Renderer.WriteLine($"- v{version.Version}, published {version.Created.ToLocalTime().ToString("D")}: {version.Notes ?? "(no release notes)"}");
            }

            if (registryPackage.Description != null)
                Renderer.WriteLine($"Description: {registryPackage.Description}");
            if (registryPackage.Tags != null)
                Renderer.WriteLine($"Tags: {string.Join(", ", registryPackage.Tags)}");
            if (registryPackage.Repository != null)
                Renderer.WriteLine($"Repository: {registryPackage.Repository}");
            if (registryPackage.Homepage != null)
                Renderer.WriteLine($"Homepage: {registryPackage.Homepage}");

            Renderer.WriteLine($"Author: {registryPackage.Author}");
            var registryAuthor = registry.Authors?.FirstOrDefault(a => a.Name == registryPackage.Author);
            if (registryAuthor != null)
            {
                if (registryAuthor.Github != null)
                    Renderer.WriteLine($"- Github: {registryAuthor.Github}");
                if (registryAuthor.Reddit != null)
                    Renderer.WriteLine($"- Reddit: {registryAuthor.Reddit}");
            }

            Renderer.WriteLine("Files:");
            foreach (var file in registryVersion.Files.Where(f => !f.Ignore && f.Filename != null))
            {
                Renderer.WriteLine($"- {file.Filename}: {file.Url ?? "not available in registry"}");
            }
        }
    }
}
