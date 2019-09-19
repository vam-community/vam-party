using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Local;

namespace Party.CLI.Commands
{
    public class CleanCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("clean", "Updates scenes to reference scripts from their expected folder. You can specify a specific script or scene to clean.");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filter") { Arity = ArgumentArity.ZeroOrOne, Description = "Optional package name or file to clean" });
            command.AddOption(new Option("--all", "Upgrade everything").WithAlias("-a"));
            command.AddOption(new Option("--errors", "Show warnings such as broken scenes or missing scripts").WithAlias("-e"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--delete-unused", "Deletes unused scripts").WithAlias("-d"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene").WithAlias("-v"));

            command.Handler = CommandHandler.Create<CleanArguments>(async args =>
            {
                await new CleanCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class CleanArguments : CommonArguments
        {
            public string Filter { get; set; }
            public bool All { get; set; }
            public bool Errors { get; set; }
            public bool DeleteUnused { get; set; }
            public bool Noop { get; set; }
            public bool Verbose { get; set; }
        }

        public CleanCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(CleanArguments args)
        {
            Controller.HealthCheck();

            if (args.All && args.Filter != null)
                throw new UserInputException("You cannot specify --all and an item to upgrade at the same time");
            else if (!args.All && args.Filter == null)
                throw new UserInputException("You must specify what to upgrade (a .cs, .cslist, .json or package name), or pass --all to upgrade everything");

            // TODO: If the item is a package (no extension), resolve it to a path (if the plugin was not downloaded, throw)
            var (saves, registry) = await ScanLocalFilesAndAcquireRegistryAsync(args.Filter);

            var matches = Controller.MatchLocalFilesToRegistry(saves, registry);

            foreach (var match in matches.HashMatches)
            {
                await HandleOne(match, args);
            }

            if (args.DeleteUnused)
            {
                Renderer.WriteLine("Cleaning up unused scripts...");
                saves = await ScanLocalFilesAsync(args.Filter);
                foreach (var script in saves.Scripts.Where(s => s.Scenes != null && s.Scenes.Count > 0))
                {
                    if (script is LocalScriptListFile scriptList && scriptList.Scripts != null)
                    {
                        foreach (var subscript in scriptList.Scripts)
                        {
                            DeleteScript(args, subscript);
                        }
                    }

                    DeleteScript(args, script);
                }
            }
        }

        private void DeleteScript(CleanArguments args, LocalScriptFile subscript)
        {
            if (args.Noop)
            {
                Renderer.WriteLine($"Skipping deleting because --noop was specified: {Controller.GetDisplayPath(subscript.FullPath)}", ConsoleColor.Yellow);
            }
            else
            {
                Renderer.WriteLine($"Deleting {Controller.GetDisplayPath(subscript.FullPath)}");
                Controller.Delete(subscript.FullPath);
            }
        }

        private async Task HandleOne(RegistrySavesMatch match, CleanArguments args)
        {
            if (match.Local.Scenes == null || match.Local.Scenes.Count == 0)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null);
                    PrintScanErrors(args.Errors, match.Local);
                    Renderer.WriteLine($"  Skipping because no scenes are using it", ConsoleColor.DarkGray);
                }
                return;
            }

            PrintScriptToPackage(match, null);
            PrintScanErrors(args.Errors, match.Local);

            var info = await Controller.GetInstalledPackageInfoAsync(match.Remote);

            if (info.Installed)
            {
                Renderer.WriteLine("  Already in the correct location");
            }
            else if (!info.Installable)
            {
                Renderer.WriteLine("  Skipped because the plugin is not installable.");
                return;
            }
            else
            {
                // TODO: It's already installed, we should just move the files
                info = await Controller.InstallPackageAsync(info, true);
                if (!info.Installed)
                {
                    Renderer.WriteLine($"  Failed to install package: {string.Join(", ", info.Files.Select(f => $"{Controller.GetDisplayPath(f.FullPath)}: {f.Status}"))}", ConsoleColor.Red);
                    return;
                }
            }

            Renderer.Write("  Script should bet at ", ConsoleColor.DarkGray);
            Renderer.Write(info.PackageFolder, ConsoleColor.DarkBlue);
            Renderer.WriteLine();

            foreach (var scene in match.Local.Scenes)
            {
                string scenePath = Controller.GetDisplayPath(scene.FullPath);
                if (args.Noop)
                {
                    Renderer.WriteLine($"  Skipping scene {scenePath} because --noop option was specified", ConsoleColor.Yellow);
                    continue;
                }

                Renderer.Write($"  Updating scene ");
                Renderer.Write(scenePath, ConsoleColor.Blue);
                Renderer.Write($"...");

                var changes = await Controller.ApplyNormalizedPathsToSceneAsync(scene, match.Local, info);

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

                if (changes.Length > 0)
                {
                    if (args.Noop)
                    {
                        Renderer.WriteLine("  Skipping deleting old files because the --noop option was specified", ConsoleColor.Yellow);
                    }
                    else if (args.Filter == null)
                    {
                        Controller.Delete(match.Local.FullPath);
                    }
                }
            }
        }
    }
}
