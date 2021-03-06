﻿using System;
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
    public class UpgradeCommand : CommandBase<UpgradeArguments>
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("upgrade", "Updates scenes to reference scripts from the Party folder. You can specify a package, scene or script to upgrade. If you don't specify anything, all scenes and scripts will be upgraded.");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filter") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option("--all", "Upgrade everything").WithAlias("-a"));
            command.AddOption(new Option("--major", "Allows upgrading even with major versions").WithAlias("-m"));
            command.AddOption(new Option("--errors", "Show warnings such as broken scenes or missing scripts").WithAlias("-e"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene").WithAlias("-v"));

            command.Handler = CommandHandler.Create<UpgradeArguments>(async args =>
            {
                await new UpgradeCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        public UpgradeCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        protected override async Task ExecuteImplAsync(UpgradeArguments args)
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
                var info = await TryInstallUpdate(match, args, upgraded);
                if (info == null) continue;
                if (!upgraded.ContainsKey(match.Remote))
                    upgraded.Add(match.Remote, info);
                updatedPackagesCounter++;
            }

            if (updatedPackagesCounter == 0)
            {
                Renderer.WriteLine("No packages to update!");
                return;
            }

            Renderer.WriteLine($"Finished upgrading {updatedPackagesCounter} packages");
            Renderer.WriteLine("Upgrading scenes...");

            var upgradedScenesCounter = 0;

            foreach (var scene in matches.HashMatches.SelectMany(m => m.Local.Scenes.Select(x => (match: m, scene: x))).GroupBy(x => x.scene).Distinct())
            {
                if (await UpgradeScene(scene.Key, scene.Select(s => s.match).Distinct(), upgraded))
                    upgradedScenesCounter++;
            }

            if (upgradedScenesCounter > 0)
                Renderer.WriteLine($"Finished upgrading {upgradedScenesCounter} scenes");
            else
                Renderer.WriteLine("No scenes to upgrade!");
        }

        private async Task<bool> UpgradeScene(LocalSceneFile scene, IEnumerable<RegistrySavesMatch> matches, IDictionary<RegistryPackageVersionContext, LocalPackageInfo> upgraded)
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
                    Renderer.WriteLine($"  Skipping unused package", ConsoleColor.DarkGray);
                }
                return null;
            }

            if (upgraded.ContainsKey(match.Remote))
            {
                Renderer.WriteLine("  Handled in a previous upgrade");
                return upgraded[match.Remote];
            }

            var latestCompatVersion = match.Remote.Package.GetLatestVersionCompatibleWith(match.Remote.Version.Version);
            var latestVersion = match.Remote.Package.GetLatestVersion();
            var updateToVersion = args.Major
                ? (latestVersion.Version.Equals(match.Remote.Version.Version) ? null : latestVersion)
                : (latestCompatVersion.Version.Equals(match.Remote.Version.Version) ? null : latestCompatVersion);

            if (updateToVersion == null)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null, null);
                    PrintScanErrors(args.Errors, match.Local);
                    Renderer.WriteLine($"  No updates available", ConsoleColor.DarkGray);
                }
                return null;
            }

            PrintScriptToPackage(match, updateToVersion, latestVersion);
            PrintScanErrors(args.Errors, match.Local);

            var info = await Controller.GetInstalledPackageInfoAsync(match.Remote.WithVersion(updateToVersion ?? match.Remote.Version));

            if (info.Installed)
            {
                Renderer.WriteLine("  Already installed");
                return info;
            }

            if (!args.Force && !info.Installable)
            {
                Renderer.WriteLine("  This plugin cannot be installed automatically.", ConsoleColor.Yellow);
                Renderer.WriteLine($"  You can instead download at {match.Remote.Version.DownloadUrl ?? match.Remote.Package.Homepage ?? match.Remote.Package.Repository ?? "(no download link provided)"}");
                Renderer.WriteLine("  Files:");
                PrintInstalledFiles(info, "  ");
                return null;
            }
            if (!args.Force && info.Corrupted)
            {
                Renderer.WriteLine("  Locally installed version does not match the registry.", ConsoleColor.Red);
                Renderer.WriteLine($"  You can instead download at {match.Remote.Version.DownloadUrl ?? match.Remote.Package.Homepage ?? match.Remote.Package.Repository ?? "(no download link provided)"}");
                Renderer.WriteLine("  Files:");
                PrintInstalledFiles(info, "  ");
                return null;
            }

            if (args.Noop)
            {
                Renderer.WriteLine("skipping install because the --noop option was specified", ConsoleColor.Yellow);
                return null;
            }

            Renderer.WriteLine($"  Downloading... ");
            info = await Controller.InstallPackageAsync(info, args.Force);
            if (info.Installed)
            {
                Renderer.WriteLine($"  Installed in {Controller.GetDisplayPath(info.PackageFolder)}:", ConsoleColor.Green);
            }
            if (!args.Force && !info.Installable)
            {
                Renderer.WriteLine("  This plugin could be installed automatically.", ConsoleColor.Yellow);
                Renderer.WriteLine($"  You can instead download at {match.Remote.Version.DownloadUrl ?? match.Remote.Package.Homepage ?? match.Remote.Package.Repository ?? "(no download link provided)"}");
                Renderer.WriteLine("  Files:");
                PrintInstalledFiles(info, "  ");
                return null;
            }
            if (!args.Force && info.Corrupted)
            {
                Renderer.WriteLine("  The download files did not match the registry hash.", ConsoleColor.Red);
                Renderer.WriteLine($"  You can instead download at {match.Remote.Version.DownloadUrl ?? match.Remote.Package.Homepage ?? match.Remote.Package.Repository ?? "(no download link provided)"}");
                Renderer.WriteLine("  Files:");
                PrintInstalledFiles(info, "  ");
                return null;
            }
            return info;
        }
    }

    public class UpgradeArguments : CommonArguments
    {
        public string Filter { get; set; }
        public bool All { get; set; }
        public bool Major { get; set; }
        public bool Errors { get; set; }
        public bool Noop { get; set; }
        public bool Verbose { get; set; }
    }
}
