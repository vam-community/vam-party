using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models;

namespace Party.CLI.Commands
{
    public class UpgradeCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("upgrade", "Updates scenes to reference scripts from the Party folder. You can specify a package, scene or script to upgrade. If you don't specify anything, all scenes and scripts will be upgraded.");
            AddCommonOptions(command);
            // TODO: Specify specific scenes and/or specific scripts and/or specific packages to upgrade
            command.AddArgument(new Argument<string>("filters") { Arity = ArgumentArity.ZeroOrMore });
            command.AddOption(new Option("--all", "Upgrade everything"));
            command.AddOption(new Option("--get", "Downloads registered scripts that were not already downloaded"));
            command.AddOption(new Option("--fix", "Updates scenes referencing scripts that are not yet in the party folder"));
            command.AddOption(new Option("--clean", "Deletes the source script after scenes have been updated"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene"));

            command.Handler = CommandHandler.Create<UpgradeArguments>(async args =>
            {
                await new UpgradeCommand(renderer, config, args.Saves, controller).ExecuteAsync(args);
            });
            return command;
        }
        public class UpgradeArguments : CommonArguments
        {
            public string[] Filters { get; set; }
            public bool All { get; set; }
            public bool Get { get; set; }
            public bool Fix { get; set; }
            public bool Clean { get; set; }
            public bool Warnings { get; set; }
            public bool Noop { get; set; }
            public bool Verbose { get; set; }
        }

        public UpgradeCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(UpgradeArguments args)
        {
            if (args.All && args.Filters != null)
                throw new UserInputException("You cannot specify --all and an item to upgrade at the same time");
            else if (!args.All && args.Filters == null)
                throw new UserInputException("You must specify what to upgrade (a .cs, .cslist, .json or package name), or pass --all to upgrade everything");

            // TODO: If the item is a package (no extension), resolve it to a path (if the plugin was not downloaded, throw)
            Renderer.WriteLine("Analyzing the saves folder and downloading the scripts list from the registry...");
            var (saves, registry) = await GetSavesAndRegistryAsync(args.Filters?.Select(Path.GetFullPath).ToArray());

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            PrintWarnings(args.Warnings, saves.Errors);

            foreach (var match in matches)
            {
                await HandleOne(match, args);
            }
        }

        private async Task HandleOne(RegistrySavesMatch match, UpgradeArguments args)
        {
            var latestVersion = match.Script.GetLatestVersion();
            var updateToVersion = latestVersion.Version.Equals(match.Version.Version) ? null : latestVersion;

            if (match.Local.Scenes == null || match.Local.Scenes.Count == 0 && updateToVersion == null)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null);
                    Renderer.WriteLine($"  Skipping because no updates are available and no scenes are using it", ConsoleColor.Yellow);
                }
                return;
            }

            PrintScriptToPackage(match, updateToVersion);

            var info = await Controller.GetInstalledPackageInfoAsync(match.Script.Name, updateToVersion ?? match.Version);

            var statuses = info.DistinctStatuses();

            if (statuses.Length != 1)
            {
                PrintCorruptedInstallInfo(info);
                return;
            }

            var status = statuses[0];

            if (status == InstalledPackageInfoResult.FileStatus.HashMismatch)
            {
                PrintCorruptedInstallInfo(info);
                return;
            }

            if (status == InstalledPackageInfoResult.FileStatus.NotInstalled)
            {
                if (args.Get)
                {
                    if (args.Noop)
                    {
                        Renderer.WriteLine("  Skipping install because the --noop option was specified", ConsoleColor.Yellow);
                    }
                    else
                    {
                        Renderer.Write($"  Installing...");
                        info = await Controller.InstallPackageAsync(info);
                        Renderer.WriteLine($"  downloaded in {info.InstallFolder}:", ConsoleColor.Green);
                        foreach (var file in info.Files)
                        {
                            Renderer.WriteLine($"  - {Controller.GetRelativePath(file.Path, info.InstallFolder)}");
                        }
                    }
                }
                else if (args.Verbose)
                {
                    Renderer.WriteLine($"  Skipping downloading to the party folder because this package is not installed (run again with --get to download it)", ConsoleColor.DarkYellow);
                }
            }
            else if (status != InstalledPackageInfoResult.FileStatus.Installed)
            {
                throw new NotImplementedException($"Status {status} is not implemented");
            }

            if (updateToVersion == null && !(args.Fix && !match.Local.FullPath.StartsWith(info.InstallFolder))) return;

            foreach (var scene in match.Local.Scenes)
            {
                string scenePath = Controller.GetRelativePath(scene.FullPath);
                if (args.Noop)
                {
                    Renderer.WriteLine($"  Skipping scene {scenePath} because --noop option was specified", ConsoleColor.Yellow);
                    continue;
                }
                Renderer.Write($"  Updating scene ");
                Renderer.Write(scenePath, ConsoleColor.Blue);
                Renderer.Write($"...");

                var changes = await Controller.UpdateScriptInSceneAsync(scene, match.Local, info);

                if (changes.Length > 0)
                    Renderer.WriteLine(" updated", ConsoleColor.Green);
                else
                    Renderer.WriteLine(" already up to date", ConsoleColor.DarkGray);

                if (args.Verbose)
                {
                    using (Renderer.WithColor(ConsoleColor.DarkGray))
                    {
                        foreach (var (before, after) in changes)
                        {
                            Renderer.WriteLine($"    {before} > {after}");
                        }
                    }
                }

                if (args.Clean && changes.Length > 0)
                {
                    if (args.Noop)
                    {
                        Renderer.WriteLine("  Skipping cleanup because the --noop option was specified", ConsoleColor.Yellow);
                    }
                    else
                    {
                        Controller.Delete(match.Local.FullPath);
                    }
                }
            }
        }

        private void PrintScriptToPackage(RegistrySavesMatch match, RegistryScriptVersion updateToVersion)
        {
            Renderer.Write($"Script ");
            Renderer.Write(Controller.GetRelativePath(match.Local.FullPath), ConsoleColor.Blue);
            Renderer.Write($" is ");
            Renderer.Write($"{match.Script.Name} v{match.Version.Version}", ConsoleColor.Cyan);
            Renderer.Write($" > ");
            if (updateToVersion == null)
            {
                Renderer.Write($"already up to date", ConsoleColor.DarkGray);
                Renderer.WriteLine();
            }
            else
            {
                Renderer.Write($"new version available: v{updateToVersion.Version}", ConsoleColor.Magenta);
                Renderer.WriteLine();
                Renderer.WriteLine($"  Version release {updateToVersion.Created.ToLocalTime().ToString("D")}: {updateToVersion.Notes ?? "No release notes"}");
            }
        }

        private void PrintCorruptedInstallInfo(InstalledPackageInfoResult info)
        {
            using (Renderer.WithColor(ConsoleColor.Red))
            {
                Renderer.WriteLine($"  Installed version in {info.InstallFolder} is corrupted.");
                foreach (var file in info.Files)
                {
                    Renderer.WriteLine($"  - {Controller.GetRelativePath(file.Path)} is {file.Status}");
                }
            }
        }
    }
}
