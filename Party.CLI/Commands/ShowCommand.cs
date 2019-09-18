using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public class ShowCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("show", "Show information about a package");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null));

            command.Handler = CommandHandler.Create<ShowArguments>(async args =>
            {
                await new ShowCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class ShowArguments : CommonArguments
        {
            public string Package { get; set; }
        }

        public ShowCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(ShowArguments args)
        {
            Controller.HealthCheck();

            if (!PackageFullName.TryParsePackage(args.Package, out var packageName))
                throw new UserInputException("Invalid package name. Example: 'scripts/my-script'");

            var registry = await Controller.GetRegistryAsync().ConfigureAwait(false);

            // TODO: Should handle other types, and and be handled in the controller
            var package = registry.Get(packageName.Type)?.FirstOrDefault(p => p.Name.Equals(packageName.Name, StringComparison.InvariantCultureIgnoreCase));

            if (package == null)
            {
                throw new UserInputException($"Could not find package {args.Package}");
            }

            var latestVersion = package.GetLatestVersion();

            if (latestVersion?.Files == null)
            {
                throw new RegistryException("Package does not have any versions");
            }

            Renderer.WriteLine($"Package {package.Type.ToString().ToLowerInvariant()}/{package.Name}");

            Renderer.WriteLine($"Last version v{latestVersion.Version}, published {latestVersion.Created.ToLocalTime().ToString("D")}");

            Renderer.WriteLine("Versions:");
            foreach (var version in package.Versions)
            {
                Renderer.WriteLine($"- v{version.Version}, published {version.Created.ToLocalTime().ToString("D")}: {version.Notes ?? "(no release notes)"}");
                if (version.DownloadUrl != null)
                    Renderer.WriteLine($"  Download: {version.DownloadUrl}");
            }

            if (package.Description != null)
                Renderer.WriteLine($"Description: {package.Description}");
            if (package.Tags != null)
                Renderer.WriteLine($"Tags: {string.Join(", ", package.Tags)}");
            if (package.Repository != null)
                Renderer.WriteLine($"Repository: {package.Repository}");
            if (package.Homepage != null)
                Renderer.WriteLine($"Homepage: {package.Homepage}");

            Renderer.WriteLine($"Author: {package.Author}");
            var registryAuthor = registry.Authors?.FirstOrDefault(a => a.Name == package.Author);
            if (registryAuthor != null)
            {
                if (registryAuthor.Github != null)
                    Renderer.WriteLine($"- Github: {registryAuthor.Github}");
                if (registryAuthor.Reddit != null)
                    Renderer.WriteLine($"- Reddit: {registryAuthor.Reddit}");
            }

            if ((latestVersion.Dependencies?.Count ?? 0) > 0)
            {
                // TODO: This should be resolved by the Controller
                Renderer.WriteLine("Dependencies:");
                foreach (var dependency in latestVersion.Dependencies.Select(d => (d, p: registry.Get(RegistryPackageType.Scripts).FirstOrDefault(p => p.Name == d.Name))))
                {
                    if (dependency.p == null)
                    {
                        Renderer.WriteLine($"- {dependency.d.Name} v{dependency.d.Version} (not found in the registry)");
                    }
                    else
                    {
                        Renderer.WriteLine($"- {dependency.d.Name} v{dependency.d.Version} by {dependency.p.Author}");
                    }
                }
            }

            Renderer.WriteLine($"Files in v{latestVersion.Version}:");
            foreach (var file in latestVersion.Files.Where(f => !f.Ignore && f.Filename != null))
            {
                Renderer.WriteLine($"- {file.Filename}: {file.Url ?? "not available in registry"}");
            }
        }
    }
}
