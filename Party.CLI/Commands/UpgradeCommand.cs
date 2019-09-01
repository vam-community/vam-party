using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;

namespace Party.CLI.Commands
{
    public class UpgradeCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("upgrade", "Updates scenes to reference scripts from the Party folder");
            AddCommonOptions(command);
            // TODO: Specify specific scenes and/or specific scripts and/or specific packages to upgrade
            command.AddOption(new Option("--get", "Downloads registered scripts that were not already downloaded"));
            command.AddOption(new Option("--fix", "Updates scenes referencing scripts that are not yet in the party folder"));
            command.AddOption(new Option("--clean", "Deletes the source script after scenes have been updated"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene"));

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, bool get, bool fix, bool clean, bool warnings, bool noop, bool verbose) =>
            {
                await new UpgradeCommand(renderer, config, saves, controller).ExecuteAsync(get, fix, clean, warnings, noop, verbose);
            });
            return command;
        }

        public UpgradeCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(bool get, bool fix, bool clean, bool warnings, bool noop, bool verbose)
        {
            Renderer.WriteLine("Analyzing the saves folder and downloading the scripts list from the registry...");
            var (saves, registry) = await GetSavesAndRegistryAsync();

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            PrintWarnings(warnings, saves.Errors);

            foreach (var match in matches)
            {
                await HandleOne(match, get, fix, clean, noop, verbose);
            }
        }

        private async Task HandleOne(RegistrySavesMatch match, bool get, bool fix, bool clean, bool noop, bool verbose)
        {
            var latestVersion = match.Script.GetLatestVersion();
            var updateToVersion = latestVersion.Version.Equals(match.Version.Version) ? null : latestVersion;

            if (match.Local.Scenes == null || match.Local.Scenes.Count == 0 && updateToVersion == null)
            {
                if (verbose)
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
                if (get)
                {
                    if (noop)
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
                else if (verbose)
                {
                    Renderer.WriteLine($"  Skipping the upgrade because this package is not installed (run again with --get to download it)", ConsoleColor.DarkYellow);
                }
            }
            else if (status != InstalledPackageInfoResult.FileStatus.Installed)
            {
                throw new NotImplementedException($"Status {status} is not implemented");
            }

            if (updateToVersion == null && !(fix && !match.Local.FullPath.StartsWith(info.InstallFolder))) return;

            foreach (var scene in match.Local.Scenes)
            {
                string scenePath = Controller.GetRelativePath(scene.FullPath);
                if (noop)
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

                if (verbose)
                {
                    using (Renderer.WithColor(ConsoleColor.DarkGray))
                    {
                        foreach (var (before, after) in changes)
                        {
                            Renderer.WriteLine($"    {before} > {after}");
                        }
                    }
                }

                if (clean && changes.Length > 0)
                {
                    if (noop)
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
                Renderer.WriteLine($"  Version release {updateToVersion.Created.ToLocalTime()}: {updateToVersion.Notes ?? "No release notes"}");
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
