using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;

namespace Party.CLI.Commands
{
    public class GetCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer output, PartyConfiguration config, PartyController controller)
        {
            var command = new Command("get", "Downloads a package (script) into the saves folder");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null) { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option("--version", "Choose a specific version to install") { Argument = new Argument<string>("version", null) });
            command.AddOption(new Option("--noop", "Do not install, just check what it will do"));

            command.Handler = CommandHandler.Create(async (string saves, string package, string version, bool noop) =>
            {
                await new GetCommand(output, config, saves, controller).ExecuteAsync(package, version, noop);
            });
            return command;
        }

        public GetCommand(IRenderer output, PartyConfiguration config, string saves, PartyController controller) : base(output, config, saves, controller)
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

            var filesStatuses = await Controller.GetInstalledPackageInfo(registryPackage.Name, registryPackageVersion);

            var distinctStatuses = filesStatuses.Files.Select(f => f.Status).Distinct().ToList();

            if (distinctStatuses.Count == 1)
            {
                switch (distinctStatuses.FirstOrDefault())
                {
                    case Shared.Results.InstalledPackageInfoResult.FileStatus.Installed:
                        throw new UserInputException("Plugin already installed");
                    case Shared.Results.InstalledPackageInfoResult.FileStatus.HashMismatch:
                        throw new PackageInstallationException("Installed plugin does not match the registry version. Did you modified it?");
                }
            }
            else if (distinctStatuses.Count > 1)
            {
                throw new PackageInstallationException("The installed plugin has been either partially installed or was modified. Try deleting the installed package folder and try again.");
            }

            if (noop)
            {
                await Output.WriteLineAsync($"Package {registryPackage.Name} v{registryPackageVersion.Version} by {registryPackage.Author.Name ?? "Anonymous"}");
                await Output.WriteLineAsync($"Files will be downloaded in {filesStatuses.InstallFolder}:");
                foreach (var file in filesStatuses.Files)
                {
                    await Output.WriteLineAsync($"- Path: {Controller.GetRelativePath(file.Path, filesStatuses.InstallFolder)}");
                    await Output.WriteLineAsync($"  Hash: {file.RegistryFile.Hash} ({file.RegistryFile.Hash.Type})");
                    await Output.WriteLineAsync($"  Url:  {file.RegistryFile.Url}");
                }
                return;
            }

            throw new NotImplementedException("");
        }
    }
}
