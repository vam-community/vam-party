using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Resources;

namespace Party.CLI.Commands
{
    public class StatusCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("status", "Shows the state of the current scripts and scenes");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filters") { Arity = ArgumentArity.ZeroOrMore });
            command.AddOption(new Option("--scenes", "Show scenes information"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--unregistered", "Show all scripts that were not registered"));

            command.Handler = CommandHandler.Create<StatusArguments>(async args =>
            {
                await new StatusCommand(renderer, config, controller, args).ExecuteAsync(args);
            });
            return command;
        }

        public class StatusArguments : CommonArguments
        {
            public string[] Filters { get; set; }
            public bool Scenes { get; set; }
            public bool Warnings { get; set; }
            public bool Unregistered { get; set; }
        }

        public StatusCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
            : base(renderer, config, controller, args)
        {
        }

        private async Task ExecuteAsync(StatusArguments args)
        {
            Controller.HealthCheck();

            Renderer.WriteLine("Analyzing the saves folder and downloading the scripts list from the registry...");
            var (saves, registry) = await GetSavesAndRegistryAsync(args.Filters);

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            PrintWarnings(args.Warnings, saves.Errors);

            foreach (var match in matches.OrderBy(m => m.Script.Name).ThenBy(m => m.Version))
            {
                Renderer.Write(match.Script.Name, ConsoleColor.Green);
                Renderer.Write(" ");
                Renderer.Write($"v{match.Version.Version}", ConsoleColor.Gray);
                if (!match.Local.FullPath.StartsWith(Config.Scanning.PackagesFolder))
                {
                    Renderer.Write(" ");
                    Renderer.Write($"\"{Controller.GetDisplayPath(match.Local.FullPath)}\"", ConsoleColor.DarkGray);
                }
                Renderer.Write(" ");
                Renderer.Write($"referenced by {Pluralize(match.Local.Scenes?.Count() ?? 0, "scene", "scenes")}", ConsoleColor.DarkCyan);
                var latestVersion = match.Script.GetLatestVersion();
                if (match.Version != latestVersion)
                    Renderer.Write($" [update available: v{latestVersion.Version}]", ConsoleColor.Blue);
                Renderer.WriteLine();
                if (args.Scenes) PrintScenes(match.Local.Scenes);
            }

            if (args.Unregistered)
            {
                foreach (var script in saves.Scripts.Where(s => !matches.Any(m => m.Local == s)).OrderBy(s => s.Name))
                {
                    Renderer.Write(script.Name, ConsoleColor.Red);
                    Renderer.Write(" ");
                    Renderer.Write($"\"{Controller.GetDisplayPath(script.FullPath)}\"", ConsoleColor.DarkGray);
                    Renderer.Write(" ");
                    Renderer.Write($"referenced by {Pluralize(script.Scenes?.Count() ?? 0, "scene", "scenes")}", ConsoleColor.DarkCyan);
                    Renderer.Write(Environment.NewLine);
                    if (args.Scenes)
                        PrintScenes(script.Scenes);
                }
            }
        }

        private void PrintScenes(List<Scene> scenes)
        {
            if (scenes == null) return;
            foreach (var scene in scenes)
            {
                Renderer.WriteLine($"- {Controller.GetDisplayPath(scene.FullPath)}");
            }
        }
    }
}
