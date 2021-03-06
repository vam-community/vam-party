﻿using System;
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
    public class GetCommand : CommandBase<GetArguments>
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("get", "Downloads a package (script, morph or scene) into the saves folder");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("packages", null) { Description = "The package, in the format scripts/name or scripts/name@1.0.0", Arity = ArgumentArity.ZeroOrMore });
            command.AddOption(new Option("--noop", "Do not install, just check what it will do"));
            command.AddOption(new Option("--all", "Install the latest version of everything").WithAlias("-a"));

            command.Handler = CommandHandler.Create<GetArguments>(async args =>
            {
                await new GetCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        public GetCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        protected override async Task ExecuteImplAsync(GetArguments args)
        {
            ValidateArguments(args.Packages);
            Controller.HealthCheck();

            var packages = args.Packages ?? new string[0];

            if (args.All && packages.Length > 0)
                throw new UserInputException("You cannot specify --all and an item to get at the same time");
            else if (!args.All && packages.Length == 0)
                throw new UserInputException("You must specify what to get, or pass --all to get everything");

            var registry = await Controller.AcquireRegistryAsync().ConfigureAwait(false);

            if (args.All)
            {
                foreach (var package in registry.Get(RegistryPackageType.Scripts))
                {
                    await GetOneAsync(args, new RegistryPackageVersionContext(registry, package, package.GetLatestVersion()));
                }
            }
            else
            {
                foreach (var p in packages)
                {
                    if (!PackageFullName.TryParsePackage(p, out var packageName))
                        throw new UserInputException("Invalid package name. Example: 'scripts/my-script'");

                    var package = registry.GetPackage(packageName);
                    if (package == null)
                        throw new RegistryException($"Package not found: '{packageName}'");

                    var version = packageName.Version != null
                        ? package.GetVersion(packageName.Version)
                        : package.GetLatestVersion();
                    if (version == null)
                        throw new RegistryException($"Package version not found: '{packageName}'");

                    var context = new RegistryPackageVersionContext(registry, package, version);

                    await GetOneAsync(args, context);
                }
            }
        }

        private async Task GetOneAsync(GetArguments args, RegistryPackageVersionContext context)
        {
            Renderer.Write($"Installing package ");
            Renderer.Write($"{context.Package.Name} v{context.Version.Version}", ConsoleColor.Cyan);
            Renderer.WriteLine("...");

            var installedStatus = await Controller.GetInstalledPackageInfoAsync(context);

            if (installedStatus.Installed && !args.Force)
            {
                Renderer.WriteLine($"  Plugin already installed at {installedStatus.PackageFolder}", ConsoleColor.Yellow);
                PrintInstalledFiles(installedStatus);
                return;
            }

            if (installedStatus.Installable || args.Force)
            {
                if (!args.Noop)
                {
                    var installResult = await Controller.InstallPackageAsync(installedStatus, args.Force);

                    Renderer.WriteLine($"  Files downloaded in {installedStatus.PackageFolder}:");
                    PrintInstalledFiles(installResult, "  ");
                }
                else
                {
                    Renderer.WriteLine($"  Noop has been used, skipping install. Files would have been downloaded in {installedStatus.PackageFolder}:");
                    foreach (var file in installedStatus.Files.Where(f => f.Status == FileStatus.NotInstalled))
                    {
                        Renderer.WriteLine($"    Path: {Controller.GetDisplayPath(file.FullPath)}");
                        Renderer.WriteLine($"    Hash: {file.RegistryFile.Hash.Value} ({file.RegistryFile.Hash.Type})");
                        Renderer.WriteLine($"    Url:  {file.RegistryFile.Url}");
                    }
                }
                return;
            }

            Renderer.WriteLine($"  Some files are not available for download or invalid, you can instead download at {context.Version.DownloadUrl ?? context.Package.Homepage ?? context.Package.Repository ?? "(no link provided)"}", ConsoleColor.Yellow);
            PrintInstalledFiles(installedStatus, "  ");
            return;
        }
    }

    public class GetArguments : CommonArguments
    {
        public string[] Packages { get; set; }
        public bool Noop { get; set; }
        public bool All { get; set; }
    }
}
