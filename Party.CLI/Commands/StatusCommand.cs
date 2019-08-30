using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;
using Party.Shared.Resources;

namespace Party.CLI.Commands
{
    public class StatusCommand : CommandBase
    {
        public static Command CreateCommand(IRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("status", "Shows the state of the current scripts and scenes");
            AddCommonOptions(command);
            command.AddOption(new Option("--scenes", "Show scenes information"));
            command.AddOption(new Option("--warnings", "Show warnings such as broken scenes or missing scripts"));
            command.AddOption(new Option("--unregistered", "Show all scripts that were not registered"));

            command.Handler = CommandHandler.Create(async (DirectoryInfo saves, bool scenes, bool warnings, bool unregistered) =>
            {
                await new StatusCommand(renderer, config, saves, controller).ExecuteAsync(scenes, warnings, unregistered);
            });
            return command;
        }

        public StatusCommand(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller) : base(renderer, config, saves, controller)
        {
        }

        private async Task ExecuteAsync(bool scenes, bool warnings, bool unregistered)
        {
            Renderer.WriteLine("Analyzing the saves folder and downloading the scripts list from the registry...");
            var (saves, registry) = await GetSavesAndRegistry();

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            if (warnings)
                PrintWarnings(saves.Errors);
            else
                PrintWarningsCount(saves.Errors);

            foreach (var match in matches)
            {
                Renderer.Write(match.Script.Name, ConsoleColor.Green);
                Renderer.Write(" ");
                Renderer.Write(match.Version.Version, ConsoleColor.Gray);
                Renderer.Write(" ");
                Renderer.Write($"\"match.File.Filename\"", ConsoleColor.DarkGray);
                Renderer.Write(" ");
                Renderer.Write($"referenced by {Pluralize(match.Local.Scenes?.Count() ?? 0, "scene", "scenes")}", ConsoleColor.DarkCyan);
                Renderer.Write(Environment.NewLine);
                if (scenes) PrintScenes(match.Local.Scenes);
            }

            if (unregistered)
            {
                foreach (var script in saves.Scripts.Where(s => !matches.Any(m => m.Local == s)))
                {
                    Renderer.Write(script.Name, ConsoleColor.Red);
                    Renderer.Write(" ");
                    Renderer.Write($"referenced by {Pluralize(script.Scenes?.Count() ?? 0, "scene", "scenes")}", ConsoleColor.DarkCyan);
                    Renderer.Write(Environment.NewLine);
                    if (scenes)
                        PrintScenes(script.Scenes);
                }
            }
        }

        private void PrintScenes(List<Scene> scenes)
        {
            if (scenes == null) return;
            foreach (var scene in scenes)
            {
                Renderer.WriteLine($"- {Controller.GetRelativePath(scene.FullPath)}");
            }
        }

        private async Task<(SavesMap, Registry)> GetSavesAndRegistry()
        {
            var registryTask = Controller.GetRegistryAsync();
            var savesTask = Controller.GetSavesAsync();

            await Task.WhenAll();

            var registry = await registryTask;
            var saves = await savesTask;
            return (saves, registry);
        }
    }
}
