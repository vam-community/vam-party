using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;

namespace Party.CLI.Commands
{
    public class FixCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("fix", "Automatically updates scenes to reference scripts from Party");
            AddCommonOptions(command);
            command.AddOption(new Option("--get", "Downloads registered scripts that were not already downloaded"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, bool get, bool warnings, bool noop) =>
            {
                await new FixCommand(renderer, config, saves, controller).ExecuteAsync(get, warnings, noop);
            });
            return command;
        }

        public FixCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(bool get, bool warnings, bool noop)
        {
            Renderer.WriteLine("Analyzing the saves folder and downloading the scripts list from the registry...");
            var (saves, registry) = await GetSavesAndRegistryAsync();

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            PrintWarnings(warnings, saves.Errors);

            foreach (var match in matches.Where(m => m.Local.Scenes != null && m.Local.Scenes.Count > 0))
            {
                await HandleOne(match, get, noop);
                Renderer.WriteLine();
            }
        }

        private async Task HandleOne(RegistrySavesMatch match, bool get, bool noop)
        {
            Renderer.WriteLine($"Found script \"{Controller.GetRelativePath(match.Local.FullPath)}\" -> package {match.Script.Name} v{match.Version.Version}");

            var info = await Controller.GetInstalledPackageInfoAsync(match.Script.Name, match.Version);

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
                        Renderer.WriteLine($"  Installing...", ConsoleColor.Blue);
                        var installResult = await Controller.InstallPackageAsync(info);
                        Renderer.WriteLine($"  Package downloaded in {info.InstallFolder}:");
                        foreach (var file in installResult.Files)
                        {
                            Renderer.WriteLine($"  - {Controller.GetRelativePath(file.Path, info.InstallFolder)}");
                        }
                    }
                }
                else
                {
                    Renderer.WriteLine($"  Skipping because this package is not installed (run again with --get to download it)", ConsoleColor.Yellow);
                    return;
                }
            }
            else if (status != InstalledPackageInfoResult.FileStatus.Installed)
            {
                throw new NotImplementedException($"Status {status} is not implemented");
            }

            foreach (var scene in match.Local.Scenes)
            {
                // TODO: Fix scene
                Renderer.Write($"  Updating scene \"{Controller.GetRelativePath(scene.FullPath)}\"...");
                Renderer.WriteLine(" Not implemented yet.", ConsoleColor.Blue);
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
