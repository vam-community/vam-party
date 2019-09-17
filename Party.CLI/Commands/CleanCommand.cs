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
    public class CleanCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("clean", "Updates scenes to reference scripts from their expected folder. You can specify a specific script or scene to clean.");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filter") { Description = "Optional package name or file to clean" });
            command.AddOption(new Option("--all", "Upgrade everything"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--noop", "Prints what the script will do, but won't actually do anything"));
            command.AddOption(new Option("--verbose", "Prints every change that will be done on every scene"));

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
            public bool Warnings { get; set; }
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
            var (saves, registry) = await GetSavesAndRegistryAsync(args.Filter);

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            foreach (var match in matches.HashMatches)
            {
                await HandleOne(match, args);
            }
        }

        private async Task HandleOne(RegistrySavesMatch match, CleanArguments args)
        {
            if (match.Local.Scenes == null || match.Local.Scenes.Count == 0)
            {
                if (args.Verbose)
                {
                    PrintScriptToPackage(match, null);
                    PrintWarnings(args.Warnings, match.Local);
                    Renderer.WriteLine($"  Skipping because no scenes are using it", ConsoleColor.DarkGray);
                }
                return;
            }

            PrintScriptToPackage(match, null);
            PrintWarnings(args.Warnings, match.Local);

            var info = await Controller.GetInstalledPackageInfoAsync(match.Package, match.Version);

            if (info.Installed)
            {
                Renderer.WriteLine("  Already in the correct location");
                return;
            }

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
