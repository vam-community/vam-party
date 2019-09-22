using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public class UpgradeCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("upgrade", "Updates scenes to reference scripts from the Party folder. You can specify a package, scene or script to upgrade. If you don't specify anything, all scenes and scripts will be upgraded.");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filter") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option("--all", "Upgrade everything").WithAlias("-a"));
            command.AddOption(new Option("--errors", "Show warnings such as broken scenes or missing scripts").WithAlias("-e"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene").WithAlias("-v"));

            command.Handler = CommandHandler.Create<UpgradeArguments>(async args =>
            {
                await new UpgradeCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        public class UpgradeArguments : CommonArguments
        {
            public string Filter { get; set; }
            public bool All { get; set; }
            public bool Errors { get; set; }
            public bool Noop { get; set; }
            public bool Verbose { get; set; }
        }

        public UpgradeCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        private async Task ExecuteAsync(UpgradeArguments args)
        {
            ValidateArguments(args.Filter);
            Controller.HealthCheck();

            if (args.All && args.Filter != null)
                throw new UserInputException("You cannot specify --all and an item to upgrade at the same time");
            else if (!args.All && args.Filter == null)
                throw new UserInputException("You must specify what to upgrade (a .cs, .cslist, .json or package name), or pass --all to upgrade everything");

            var (saves, registry) = await ScanLocalFilesAndAcquireRegistryAsync(args.Filter);

            var matches = Controller.MatchLocalFilesToRegistry(saves, registry);

            PrintScanErrors(args.Errors, saves);

            Renderer.WriteLine("Upgrading packages...");

            var updatedPackagesCounter = 0;
            var upgraded = new Dictionary<RegistryPackageVersionContext, LocalPackageInfo>();

            foreach (var match in matches.HashMatches)
            {
                await TryInstallUpdate(match, args, upgraded);
            }

            if (updatedPackagesCounter > 0)
                Renderer.WriteLine($"Finished upgrading {updatedPackagesCounter} packages");
            else
                Renderer.WriteLine("No packages to update!");

            Renderer.WriteLine("Upgrading scenes...");

            var upgradedScenesCounter = 0;

            foreach (var scene in matches.HashMatches.SelectMany(m => m.Local.Scenes.Select(x => (match: m, scene: x))).GroupBy(x => x.scene).Distinct())
            {
                if (await UpgradeScene(scene.Key, scene.Select(s => s.match).Distinct(), args, upgraded))
                    upgradedScenesCounter++;
            }

            if (upgradedScenesCounter > 0)
                Renderer.WriteLine($"Finished upgrading {upgradedScenesCounter} scenes");
            else
                Renderer.WriteLine("No scenes to upgrade!");
        }

        private async Task<bool> UpgradeScene(LocalSceneFile scene, IEnumerable<RegistrySavesMatch> matches, UpgradeArguments args, IDictionary<RegistryPackageVersionContext, LocalPackageInfo> upgraded)
        {
            Renderer.Write($"Updating scene ");
            Renderer.Write(Controller.GetDisplayPath(scene.FullPath), ConsoleColor.Blue);
            Renderer.Write($"... ");

            var changes = 0;
            foreach (var match in matches)
            {
                if (upgraded.TryGetValue(match.Remote, out var info))
                    changes += await Controller.UpgradeSceneAsync(scene, match.Local, info);
            }

            if (changes > 0)
                Renderer.WriteLine("  Scene updated", ConsoleColor.Green);
            else
                Renderer.WriteLine("  Scene already up to date", ConsoleColor.DarkGray);

            return true;
        }

        private async Task<LocalPackageInfo> TryInstallUpdate(RegistrySavesMatch match, UpgradeArguments args, IDictionary<RegistryPackageVersionContext, LocalPackageInfo> upgraded)
        {
            // No reason to upgrade since nothing uses it
            if (match.Local.Scenes == null || match.Local.Scenes.Count == 0)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null, null);
                    PrintScanErrors(args.Errors, match.Local);
                    Renderer.WriteLine($"  Skipping because unused", ConsoleColor.DarkGray);
                }
                return null;
            }

            var latestCompatVersion = match.Remote.Package.GetLatestVersionCompatibleWith(match.Remote.Version.Version);
            var latestVersion = match.Remote.Package.GetLatestVersion();
            var updateToVersion = args.Force
                ? (latestVersion.Version.Equals(match.Remote.Version.Version) ? null : latestVersion)
                : (latestCompatVersion.Version.Equals(match.Remote.Version.Version) ? null : latestCompatVersion);

            if (updateToVersion == null)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null, null);
                    PrintScanErrors(args.Errors, match.Local);
                    Renderer.WriteLine($"  Skipping because no updates are available", ConsoleColor.DarkGray);
                }
                return null;
            }

            PrintScriptToPackage(match, updateToVersion, latestVersion);
            PrintScanErrors(args.Errors, match.Local);

            var info = await Controller.GetInstalledPackageInfoAsync(match.Remote.WithVersion(updateToVersion ?? match.Remote.Version));

            if (info.Installed)
            {
                Renderer.WriteLine("  Already installed");
                return null;
            }

            if (!args.Force && (info.Corrupted || !info.Installable))
            {
                Renderer.WriteLine("  Cannot upgrade because at least one file is either broken or not downloadable.");
                Renderer.WriteLine($"  You can instead download it at {match.Remote.Version.DownloadUrl ?? match.Remote.Package.Homepage ?? match.Remote.Package.Repository ?? "(no link provided)"}");
                Renderer.WriteLine("  Files:");
                PrintInstalledFiles(info, "  ");
                if (!args.Force)
                    return null;
            }

            if (args.Noop)
            {
                Renderer.WriteLine("skipping install because the --noop option was specified", ConsoleColor.Yellow);
                return null;
            }

            Renderer.WriteLine($"  Downloading... ");
            info = await Controller.InstallPackageAsync(info, args.Force);
            upgraded.Add(match.Remote, info);
            Renderer.WriteLine($"  Installed in {Controller.GetDisplayPath(info.PackageFolder)}:", ConsoleColor.Green);
            return info;
        }
    }
}
