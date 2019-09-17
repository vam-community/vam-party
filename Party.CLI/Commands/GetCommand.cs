using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public class GetCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("get", "Downloads a package (script, morph or scene) into the saves folder");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("package", null) { Description = "The package, in the format scripts/name or scripts/name@1.0.0", Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option("--noop", "Do not install, just check what it will do"));

            command.Handler = CommandHandler.Create<GetArguments>(async args =>
            {
                await new GetCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class GetArguments : CommonArguments
        {
            public string Package { get; set; }
            public bool Noop { get; set; }
        }

        public GetCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(GetArguments args)
        {
            Controller.HealthCheck();

            if (!PackageFullName.TryParsePackage(args.Package, out var packageName))
                throw new UserInputException("Invalid package name. Example: 'scripts/my-script'");

            var registry = await Controller.GetRegistryAsync().ConfigureAwait(false);

            var registryPackage = registry.GetPackage(packageName);
            if (registryPackage == null)
                throw new RegistryException($"Package not found: '{packageName}'");

            var registryPackageVersion = packageName.Version != null
                ? registryPackage.GetVersion(packageName.Version)
                : registryPackage.GetLatestVersion();
            if (registryPackageVersion == null)
                throw new RegistryException($"Package version not found: '{packageName}'");

            var installedStatus = await Controller.GetInstalledPackageInfoAsync(registryPackage, registryPackageVersion);

            if (installedStatus.Installed && !args.Force)
            {
                throw new UserInputException($"Plugin already installed at {installedStatus.InstallFolder}");
            }
            if (installedStatus.Installable || args.Force)
            {
                if (!args.Noop)
                {
                    var installResult = await Controller.InstallPackageAsync(installedStatus, args.Force);

                    Renderer.WriteLine($"Installed package {registryPackage.Name} v{registryPackageVersion.Version} by {registryPackage.Author ?? "?"}");
                    Renderer.WriteLine($"Files downloaded in {installedStatus.InstallFolder}:");
                    PrintInstalledFiles(installResult);
                }
                else
                {
                    Renderer.WriteLine($"Noop has been used, skipping install. Files would have been downloaded in {installedStatus.InstallFolder}:");
                    foreach (var file in installedStatus.Files.Where(f => f.Status == FileStatus.NotInstalled))
                    {
                        Renderer.WriteLine($"- Path: {Controller.GetDisplayPath(file.Path)}");
                        Renderer.WriteLine($"  Hash: {file.RegistryFile.Hash.Value} ({file.RegistryFile.Hash.Type})");
                        Renderer.WriteLine($"  Url:  {file.RegistryFile.Url}");
                    }
                }
            }
            else
            {
                Renderer.WriteLine($"Some files are not available for download or invalid, you can instead download it at {registryPackage.Homepage ?? registryPackage.Repository ?? "(no link provided)"}");
                PrintInstalledFiles(installedStatus);
                return;
            }
        }
    }
}
