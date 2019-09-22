using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public class StatusCommand : CommandBase
    {
        public static Command CreateCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory)
        {
            var command = new Command("status", "Shows the state of the current scripts and scenes");
            AddCommonOptions(command);
            command.AddArgument(new Argument<string>("filter") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option("--breakdown", "Show the list of scenes and files for each script").WithAlias("-b"));
            command.AddOption(new Option("--errors", "Show warnings such as broken scenes or missing scripts").WithAlias("-e"));
            command.AddOption(new Option("--unregistered", "Show all scripts that were not registered").WithAlias("-u"));

            command.Handler = CommandHandler.Create<StatusArguments>(async args =>
            {
                await new StatusCommand(renderer, config, controllerFactory, args).ExecuteAsync(args);
            });
            return command;
        }

        public class StatusArguments : CommonArguments
        {
            public string Filter { get; set; }
            public bool Breakdown { get; set; }
            public bool Errors { get; set; }
            public bool Unregistered { get; set; }
        }

        public StatusCommand(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
            : base(renderer, config, controllerFactory, args)
        {
        }

        private async Task ExecuteAsync(StatusArguments args)
        {
            ValidateArguments(args.Filter);
            Controller.HealthCheck();

            var (saves, registry) = await ScanLocalFilesAndAcquireRegistryAsync(args.Filter);

            var matches = Controller.MatchLocalFilesToRegistry(saves, registry);

            PrintScanErrors(args.Errors, saves);

            if (matches.HashMatches.Length == 0)
            {
                Renderer.WriteLine("No scripts where found", ConsoleColor.Red);
            }
            else
            {
                foreach (var matchScript in matches.HashMatches.GroupBy(m => m.Remote.Package).OrderBy(g => g.Key.Name))
                {
                    foreach (var matchVersion in matchScript.GroupBy(s => s.Remote.Version).OrderBy(g => g.Key.Version))
                    {
                        var localFiles = matchVersion.Select(v => v.Local).ToList();
                        PrintScript(matchScript.Key, matchVersion.Key, localFiles);
                        PrintScanErrors(args.Errors, localFiles.ToArray<LocalFile>());
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
                    PrintScript(first.Remote.Package, first.Remote.Version, files.Select(f => f.Local).ToArray());
                    PrintScanErrors(args.Errors, files.Select(f => f.Local).ToArray<LocalFile>());
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
            else
            {
                if (matches.FilenameMatches.Length > 0)
                    Renderer.WriteLine($"Also found {matches.FilenameMatches.Length} scripts with a filename matching a known script. They are either unpublished or were modified. Run with --unregistered to see them.");

                if (matches.NoMatches.Length > 0)
                    Renderer.WriteLine($"Also found {matches.NoMatches.Length} scripts that could not be matched to a registered package. Run with --unregistered to see them.");
            }
        }

        private void PrintScript(RegistryPackage script, RegistryPackageVersion version, IReadOnlyCollection<LocalScriptFile> localFiles)
        {
            Renderer.Write(script.Name, ConsoleColor.Green);

            Renderer.Write(" ");
            Renderer.Write($"v{version.Version}", ConsoleColor.Gray);

            if (localFiles.Count > 0)
            {
                var filesSummary = localFiles.Select(l => l.FullPath).Select(Controller.GetDisplayPath).OrderBy(p => p.Length).ToList();
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

            var errors = localFiles.Max(l => l.Status);
            if (errors == LocalFileErrorLevel.Warning)
                Renderer.Write(" [warnings]", ConsoleColor.Yellow);
            else if (errors >= LocalFileErrorLevel.Error)
                Renderer.Write(" [errors]", ConsoleColor.Red);

            Renderer.WriteLine();
        }

        private void PrintScenes(string indent, IEnumerable<LocalSceneFile> scenes)
        {
            if (scenes == null) return;
            foreach (var scene in scenes)
            {
                Renderer.WriteLine($"{indent}{Controller.GetDisplayPath(scene.FullPath)}");
            }
        }
    }
}
