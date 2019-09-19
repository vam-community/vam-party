using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public abstract class CommandBase
    {
        private static PartyConfiguration GetConfig(PartyConfiguration config, DirectoryInfo vam)
        {
            if (vam != null)
            {
                config.VirtAMate.VirtAMateInstallFolder = vam.FullName;
            }
            return config;
        }

        protected IConsoleRenderer Renderer { get; }
        protected PartyConfiguration Config { get; }
        protected IPartyController Controller { get; }

        protected CommandBase(IConsoleRenderer renderer, PartyConfiguration config, IPartyController controller, CommonArguments args)
        {
            Renderer = renderer;
            Config = GetConfig(config, args.VaM);
            Controller = controller;
            Controller.ChecksEnabled = args.Force;
        }

        protected static void AddCommonOptions(Command command)
        {
            command.AddOption(new Option("--vam", "Specify the Virt-A-Mate install folder") { Argument = new Argument<DirectoryInfo>().ExistingOnly() });
            command.AddOption(new Option("--force", "Ignores most security checks and health checks"));
        }

        public abstract class CommonArguments
        {
            public DirectoryInfo VaM { get; set; }
            public bool Force { get; set; }
        }

        protected async Task<SavesMap> ScanLocalFilesAsync(string filter = null)
        {
            // NOTE: When specifying --noop to status, it puts --noop in a filter, and returns nothing. Try to avoid that, or at least specify why nothing has been returned?
            // TODO: This should be done in the Controller

            Renderer.WriteLine("Analyzing the saves folder and gettings the packages list from the registry, please wait...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            SavesMap saves;
            using (var reporter = new ProgressReporter<ScanLocalFilesProgress>(StartProgress, ReportProgress, CompleteProgress))
            {
                saves = await Controller.ScanLocalFilesAsync(filter, reporter).ConfigureAwait(false);
            }
            var elapsed = stopwatch.Elapsed;
            stopwatch.Stop();

            Renderer.WriteLine($"Scanned {saves.Scenes?.Length ?? 0} scenes and {saves.Scripts?.Length ?? 0} scripts in {elapsed.TotalSeconds:0.00}s");

            return saves;
        }

        protected async Task<(SavesMap, Registry)> ScanLocalFilesAndAcquireRegistryAsync(string filter = null)
        {
            // NOTE: When specifying --noop to status, it puts --noop in a filter, and returns nothing. Try to avoid that, or at least specify why nothing has been returned?
            // TODO: This should be done in the Controller

            Renderer.WriteLine("Analyzing the saves folder and gettings the packages list from the registry, please wait...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            SavesMap saves;
            Registry registry;
            using (var reporter = new ProgressReporter<ScanLocalFilesProgress>(StartProgress, ReportProgress, CompleteProgress))
            {
                (saves, registry) = await Controller.ScanLocalFilesAndAcquireRegistryAsync(null, filter, reporter).ConfigureAwait(false);
            }
            var elapsed = stopwatch.Elapsed;
            stopwatch.Stop();

            Renderer.WriteLine($"Scanned {saves.Scenes?.Length ?? 0} scenes, {saves.Scripts?.Length ?? 0} scripts and downloaded registry in {elapsed.TotalSeconds:0.00}s");

            return (saves, registry);
        }

        protected void PrintWarnings(bool details, SavesMap map)
        {
            PrintWarnings(details, map.Scripts.Cast<LocalFile>().Concat(map.Scenes).ToArray());
        }

        protected void PrintWarnings(bool details, params LocalFile[] files)
        {
            var logs = files?.Where(f => f.Errors != null && f.Errors.Count > 0).SelectMany(f => f.Errors?.Select(e => (f, e))).ToList();
            if (logs == null || logs.Count == 0) return;

            var grouped = logs.GroupBy(fe => fe.e.Level).ToDictionary(g => g.Key, g => g.ToArray());
            grouped.TryGetValue(LocalFileErrorLevel.Error, out var errors);
            grouped.TryGetValue(LocalFileErrorLevel.Warning, out var warnings);

            if (details)
            {
                if (errors != null)
                {
                    using (Renderer.WithColor(ConsoleColor.Red))
                    {
                        Renderer.WriteLine("Errors:");
                        foreach (var (f, e) in errors)
                        {
                            Renderer.Error.WriteLine($"  {Controller.GetDisplayPath(f.FullPath)}: {e.Error}");
                        }
                    }
                    Renderer.WriteLine();
                }

                if (warnings != null)
                {
                    using (Renderer.WithColor(ConsoleColor.Yellow))
                    {
                        Renderer.WriteLine("Warnings:");
                        foreach (var (f, e) in warnings)
                        {
                            Renderer.Error.WriteLine($"  {Controller.GetDisplayPath(f.FullPath)}: {e.Error}");
                        }
                    }
                }
                Renderer.WriteLine();
            }
            else
            {
                using (Renderer.WithColor(errors.Length > 0 ? ConsoleColor.Red : ConsoleColor.Yellow))
                {
                    Renderer.Error.WriteLine($"Found {Pluralize(warnings?.Length ?? 0, "warning", "warnings")} and {Pluralize(errors?.Length ?? 0, "error", "errors")} while scanning. Run with --warnings to print them.");
                }
            }
        }

        protected void PrintInstalledFiles(LocalPackageInfo installedStatus, string indent = "")
        {
            foreach (var file in installedStatus.Files)
            {
                Renderer.Write($"{indent}- {Controller.GetDisplayPath(file.FullPath)} ");
                switch (file.Status)
                {
                    case FileStatus.NotInstalled:
                        Renderer.Write($"[not installed]", ConsoleColor.Blue);
                        break;
                    case FileStatus.Installed:
                        Renderer.Write($"[installed]", ConsoleColor.Green);
                        break;
                    case FileStatus.HashMismatch:
                        Renderer.Write($"[hash mismatch]", ConsoleColor.Red);
                        break;
                    case FileStatus.Ignored:
                        Renderer.Write($"[ignored]", ConsoleColor.DarkGray);
                        break;
                    case FileStatus.NotDownloadable:
                        Renderer.Write($"[not downloadable]", ConsoleColor.Yellow);
                        break;
                }
                Renderer.WriteLine();
            }
        }

        protected void PrintScriptToPackage(RegistrySavesMatch match, RegistryPackageVersion latestVersion)
        {
            PrintScriptToPackage(match.Remote, latestVersion, match.Local);
        }

        protected void PrintScriptToPackage(RegistryPackageFileContext context, RegistryPackageVersion latestVersion, LocalScriptFile local)
        {
            var (_, package, version) = context;
            Renderer.Write($"Script ");
            Renderer.Write(Controller.GetDisplayPath(local.FullPath), ConsoleColor.Blue);
            Renderer.Write($" is ");
            Renderer.Write($"{package.Name} v{version.Version}", ConsoleColor.Cyan);
            Renderer.Write($" > ");
            if (latestVersion == null)
            {
                Renderer.Write($"already up to date", ConsoleColor.DarkGray);
                Renderer.WriteLine();
            }
            else
            {
                Renderer.Write($"new version available: v{latestVersion.Version}", ConsoleColor.Magenta);
                Renderer.WriteLine();
                Renderer.WriteLine($"  Released {latestVersion.Created.ToLocalTime().ToString("D")}: {latestVersion.Notes ?? "No release notes"}");
            }
        }

        protected string Pluralize(int count, string singular, string plural)
        {
            if (count == 1)
            {
                return $"{count} {singular}";
            }
            else
            {
                return $"{count} {plural}";
            }
        }

        private void StartProgress()
        {
            Console.CursorVisible = false;
        }

        private void ReportProgress(ScanLocalFilesProgress progress)
        {
            Renderer.Write($"{progress.Percentage()}% ({progress.Scenes.Analyzed}/{progress.Scenes.ToAnalyze} scenes, {progress.Scripts.Analyzed}/{progress.Scripts.ToAnalyze} scripts)");
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        private void CompleteProgress()
        {
            Console.CursorVisible = false;
        }
    }
}
