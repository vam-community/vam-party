using System;
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
            ValidateArguments(args.Package);
            Controller.HealthCheck();

            if (!PackageFullName.TryParsePackage(args.Package, out var packageName))
                throw new UserInputException("Invalid package name. Example: 'scripts/my-script'");

            var registry = await Controller.AcquireRegistryAsync().ConfigureAwait(false);

            var package = registry.GetPackage(packageName);
            if (package == null)
                throw new RegistryException($"Package not found: '{packageName}'");

            var version = packageName.Version != null
                ? package.GetVersion(packageName.Version)
                : package.GetLatestVersion();
            if (version == null)
                throw new RegistryException($"Package version not found: '{packageName}'");

            var context = new RegistryPackageVersionContext(registry, package, version);

            var installedStatus = await Controller.GetInstalledPackageInfoAsync(context);

            if (installedStatus.Installed && !args.Force)
            {
                Renderer.WriteLine($"Plugin already installed at {installedStatus.PackageFolder}", ConsoleColor.Red);
                PrintInstalledFiles(installedStatus);
                return;
            }
            if (installedStatus.Installable || args.Force)
            {
                if (!args.Noop)
                {
                    var installResult = await Controller.InstallPackageAsync(installedStatus, args.Force);

                    Renderer.WriteLine($"Installed package {package.Name} v{version.Version} by {package.Author ?? "?"}");
                    Renderer.WriteLine($"Files downloaded in {installedStatus.PackageFolder}:");
                    PrintInstalledFiles(installResult);
                }
                else
                {
                    Renderer.WriteLine($"Noop has been used, skipping install. Files would have been downloaded in {installedStatus.PackageFolder}:");
                    foreach (var file in installedStatus.Files.Where(f => f.Status == FileStatus.NotInstalled))
                    {
                        Renderer.WriteLine($"- Path: {Controller.GetDisplayPath(file.FullPath)}");
                        Renderer.WriteLine($"  Hash: {file.RegistryFile.Hash.Value} ({file.RegistryFile.Hash.Type})");
                        Renderer.WriteLine($"  Url:  {file.RegistryFile.Url}");
                    }
                }
            }
            else
            {
                Renderer.WriteLine($"Some files are not available for download or invalid, you can instead download it at {version.DownloadUrl ?? package.Homepage ?? package.Repository ?? "(no link provided)"}");
                PrintInstalledFiles(installedStatus);
                return;
            }
        }
    }
}
