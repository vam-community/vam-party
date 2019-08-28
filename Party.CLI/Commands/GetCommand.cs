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
    public class GetCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("get", "Downloads a package (script) into the saves folder");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null) { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option("--version", "Choose a specific version to install") { Argument = new Argument<string>("version", null) });
            command.AddOption(new Option("--noop", "Do not install, just check what it will do"));

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, string package, string version, bool noop) =>
            {
                await new GetCommand(renderer, config, saves, controller).ExecuteAsync(package, version, noop);
            });
            return command;
        }

        public GetCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(string package, string version, bool noop)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                throw new UserInputException("You must specify a package");
            }

            var registry = await Controller.GetRegistryAsync().ConfigureAwait(false);

            var registryPackage = registry.Scripts.FirstOrDefault(s => s.Name.Equals(package, StringComparison.InvariantCultureIgnoreCase));

            if (registryPackage == null)
            {
                throw new RegistryException($"Package not found: '{package}'");
            }

            var registryPackageVersion = registryPackage.GetLatestVersion();
            if (!string.IsNullOrEmpty(version))
            {
                registryPackageVersion = registryPackage.Versions.FirstOrDefault(p => p.Version == version);
                if (registryPackageVersion == null)
                {
                    throw new RegistryException($"Package version not found: '{package}' version '{version}'");
                }
            }

            var filesStatuses = await Controller.GetInstalledPackageInfoAsync(registryPackage.Name, registryPackageVersion);

            var distinctStatuses = filesStatuses.Files.Select(f => f.Status).Distinct().ToList();

            ValidateStatuses(distinctStatuses);

            if (noop)
            {
                Renderer.WriteLine($"Package {registryPackage.Name} v{registryPackageVersion.Version} by {registryPackage.Author.Name ?? "Anonymous"}");
                Renderer.WriteLine($"Files will be downloaded in {filesStatuses.InstallFolder}:");
                foreach (var file in filesStatuses.Files)
                {
                    Renderer.WriteLine($"- Path: {Controller.GetRelativePath(file.Path, filesStatuses.InstallFolder)}");
                    Renderer.WriteLine($"  Hash: {file.RegistryFile.Hash} ({file.RegistryFile.Hash.Type})");
                    Renderer.WriteLine($"  Url:  {file.RegistryFile.Url}");
                }
                return;
            }

            var installResult = await Controller.InstallPackageAsync(filesStatuses);

            Renderer.WriteLine($"Installed package {registryPackage.Name} v{registryPackageVersion.Version} by {registryPackage.Author.Name ?? "Anonymous"}");
            Renderer.WriteLine($"Files downloaded in {filesStatuses.InstallFolder}:");
            foreach (var file in installResult.Files)
            {

                Renderer.WriteLine($"- {Controller.GetRelativePath(file.Path, filesStatuses.InstallFolder)}");
            }
        }

        private static void ValidateStatuses(System.Collections.Generic.List<Shared.Results.InstalledPackageInfoResult.FileStatus> distinctStatuses)
        {
            if (distinctStatuses.Count > 1)
            {
                throw new PackageInstallationException("The installed plugin has been either partially installed or was modified. Try deleting the installed package folder and try again.");
            }

            if (distinctStatuses.Count == 0)
            {
                throw new PackageInstallationException("No files were found in this package.");
            }

            switch (distinctStatuses.FirstOrDefault())
            {
                case Shared.Results.InstalledPackageInfoResult.FileStatus.Installed:
                    throw new UserInputException("Plugin already installed");
                case Shared.Results.InstalledPackageInfoResult.FileStatus.HashMismatch:
                    throw new PackageInstallationException("Installed plugin does not match the registry version. Did you modified it?");
                default:
                    return;
            }
        }
    }
}
