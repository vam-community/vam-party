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
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("show", "Show information about a package");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null));

            command.Handler = CommandHandler.Create<ShowArguments>(async args =>
            {
                await new ShowCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        public class ShowArguments : CommonArguments
        {
            public string Package { get; set; }
        }

        public ShowCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        private async Task ExecuteAsync(ShowArguments args)
        {
            ValidateArguments(args.Package);
            Controller.HealthCheck();

            if (!PackageFullName.TryParsePackage(args.Package, out var packageName))
                throw new UserInputException("Invalid package name. Example: 'scripts/my-script'");

            var registry = await Controller.AcquireRegistryAsync().ConfigureAwait(false);
            var package = registry.GetPackage(packageName);
            if (package == null)
                throw new UserInputException($"Could not find package {args.Package}");
            var version = package.GetLatestVersion();
            if (version?.Files == null)
                throw new RegistryException("Package does not have any versions");
            var context = new RegistryPackageVersionContext(registry, package, version);

            var packageTitle = $"{package.Name} v{version.Version} ({package.Type.ToString().ToLowerInvariant()})";
            Renderer.WriteLine(packageTitle, ConsoleColor.Cyan);
            Renderer.WriteLine(new string('=', packageTitle.Length));

            Renderer.WriteLine("Info:", ConsoleColor.Blue);
            if (package.Description != null)
                Renderer.WriteLine($"  Description: {package.Description}");
            if (package.Tags != null)
                Renderer.WriteLine($"  Tags: {string.Join(", ", package.Tags)}");
            if (package.Repository != null)
                Renderer.WriteLine($"  Repository: {package.Repository}");
            if (package.Homepage != null)
                Renderer.WriteLine($"  Homepage: {package.Homepage}");

            Renderer.Write("Author:", ConsoleColor.Blue);
            Renderer.WriteLine($" {package.Author}");
            var registryAuthor = registry.Authors?.FirstOrDefault(a => a.Name == package.Author);
            if (registryAuthor != null)
            {
                if (registryAuthor.Github != null)
                    Renderer.WriteLine($"  Github: {registryAuthor.Github}");
                if (registryAuthor.Reddit != null)
                    Renderer.WriteLine($"  Reddit: {registryAuthor.Reddit}");
            }

            Renderer.WriteLine("Versions:", ConsoleColor.Blue);
            foreach (var v in package.Versions)
            {
                Renderer.WriteLine($"  v{v.Version}, {v.Created.ToLocalTime().ToString("d")}: {v.Notes ?? "(none)"}");
                if (version.DownloadUrl != null)
                    Renderer.WriteLine($"  Download: {v.DownloadUrl}");
            }

            if ((version.Dependencies?.Count ?? 0) > 0)
            {
                Renderer.WriteLine("Dependencies:", ConsoleColor.Blue);
                foreach (var dependency in version.Dependencies)
                {
                    if (registry.TryGetDependency(dependency, out var depContext))
                    {
                        Renderer.WriteLine($"  {depContext.Package.Name} v{depContext.Version.Version} by {depContext.Package.Author} ({depContext.Version.Files.Count} files)");
                    }
                    else
                    {
                        Renderer.WriteLine($"  Dependency {dependency} was not found in the registry");
                    }
                }
            }

            Renderer.WriteLine($"Files (for v{version.Version}):", ConsoleColor.Blue);
            var info = await Controller.GetInstalledPackageInfoAsync(context);
            PrintInstalledFiles(info, "");
        }
    }
}
