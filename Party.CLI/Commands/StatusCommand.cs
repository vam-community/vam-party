using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public class StatusCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller)
        {
            var command = new Command("status", "Shows the state of the current scripts and scenes");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filter") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option("--breakdown", "Show the list of scenes and files for each script"));
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
            public string Filter { get; set; }
            public bool Breakdown { get; set; }
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

            var (saves, registry) = await GetSavesAndRegistryAsync(args.Filter);

            var matches = Controller.MatchSavesToRegistry(saves, registry);

            PrintWarnings(args.Warnings, saves.Errors);

            if (matches.HashMatches.Length == 0)
            {
                Renderer.WriteLine("No scripts where found", ConsoleColor.Red);
            }
            else
            {
                foreach (var matchScript in matches.HashMatches.GroupBy(m => m.Script).OrderBy(g => g.Key.Name))
                {
                    foreach (var matchVersion in matchScript.GroupBy(s => s.Version).OrderBy(g => g.Key.Version))
                    {
                        var localFiles = matchVersion.Select(v => v.Local).ToList();
                        PrintScript(matchScript.Key, matchVersion.Key, localFiles);
                        if (args.Breakdown)
                        {
                            foreach (var localFile in localFiles.OrderBy(p => p.FullPath))
                            {
                                Renderer.WriteLine($"- Script: {Controller.GetDisplayPath(localFile.FullPath)}");
                                PrintScenes("  - Scene: ", localFile.Scenes.ToList());
                            }
                        }
                    }
                }
            }

            if (args.Unregistered)
            {
                foreach (var match in matches.FilenameMatches.GroupBy(m => $"{m.Local.FileName}:{m.Local.Hash}"))
                {
                    var files = match.ToArray();
                    var first = files.First();
                    PrintScript(first.Script, first.Version, files.Select(f => f.Local).ToArray());
                    if (args.Breakdown)
                        PrintScenes("- ", files.SelectMany(f => f.Local.Scenes).Distinct().ToList());
                }

                foreach (var script in matches.NoMatches)
                {
                    Renderer.Write(script.FileName, ConsoleColor.Red);
                    Renderer.Write(" ");
                    Renderer.Write($"\"{Controller.GetDisplayPath(script.FullPath)}\"", ConsoleColor.DarkGray);
                    Renderer.Write(" ");
                    Renderer.Write($"referenced by {Pluralize(script.Scenes?.Count() ?? 0, "scene", "scenes")}", ConsoleColor.DarkCyan);
                    Renderer.Write(Environment.NewLine);
                    if (args.Breakdown)
                        PrintScenes("- ", script.Scenes);
                }
            }
        }

        private void PrintScript(RegistryPackage script, RegistryPackageVersion version, IReadOnlyCollection<LocalScriptFile> localFiles)
        {
            Renderer.Write(script.Name, ConsoleColor.Green);
            Renderer.Write(" ");
            Renderer.Write($"v{version.Version}", ConsoleColor.Gray);
            var nonInstalledLocalFiles = localFiles.Where(l => !l.FullPath.StartsWith(Config.VirtAMate.PackagesFolder)).ToList();
            if (nonInstalledLocalFiles.Count > 0)
            {
                var filesSummary = nonInstalledLocalFiles.Select(l => l.FullPath).Select(Controller.GetDisplayPath).OrderBy(p => p.Length).ToList();
                Renderer.Write(" ");
                if (filesSummary.Count == 1)
                {
                    Renderer.Write($"\"{filesSummary.FirstOrDefault()}\"", ConsoleColor.DarkGray);
                }
                else
                {
                    Renderer.Write($"\"{filesSummary.FirstOrDefault()}\" and {filesSummary.Count - 1} others...", ConsoleColor.DarkGray);
                }
            }
            Renderer.Write(" ");
            Renderer.Write($"referenced by {Pluralize(localFiles.Sum(l => l.Scenes?.Count() ?? 0), "scene", "scenes")}", ConsoleColor.DarkCyan);
            var latestVersion = script.GetLatestVersion();
            if (version != latestVersion)
                Renderer.Write($" [update available: v{latestVersion.Version}]", ConsoleColor.Yellow);
            else
                Renderer.Write($" [up to date]", ConsoleColor.Cyan);
            Renderer.WriteLine();
        }

        private void PrintScenes(string indent, List<LocalSceneFile> scenes)
        {
            if (scenes == null) return;
            foreach (var scene in scenes)
            {
                Renderer.WriteLine($"{indent}{Controller.GetDisplayPath(scene.FullPath)}");
            }
        }
    }
}
