﻿using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Exceptions;
using Party.Shared.Logging;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.CLI.Commands
{
    public abstract class CommandBase<T>
        where T : CommonArguments
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
        protected ILogger Logger { get; }
        protected IPartyController Controller { get; }

        protected CommandBase(IConsoleRenderer renderer, PartyConfiguration config, IPartyControllerFactory controllerFactory, CommonArguments args)
        {
            Renderer = renderer;
            Config = GetConfig(config, args.VaM);
            Logger = args.Log == LogLevel.Disabled ? (ILogger)new NullLogger() : new AccumulatorLogger(args.Log);
            Controller = controllerFactory.Create(Config, Logger, !args.Force);
        }

        public async Task ExecuteAsync(T args)
        {
            try
            {
                await ExecuteImplAsync(args);
            }
            finally
            {
                if (args.Log != LogLevel.Disabled)
                {
                    WriteLogs(args);
                }
            }
        }

        protected static void AddCommonOptions(Command command)
        {
            command.AddOption(new Option("--vam", "Specify the Virt-A-Mate install folder") { Argument = new Argument<DirectoryInfo>().ExistingOnly() });
            command.AddOption(new Option("--force", "Ignores most security checks and health checks").WithAlias("-f"));
            command.AddOption(new Option("--log", "Output log information") { Argument = new Argument<LogLevel>() });
        }

        protected abstract Task ExecuteImplAsync(T args);

        protected void ValidateArguments(params string[] values)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value != null && value.StartsWith("-"))
                    throw new UserInputException($"Unknown argument: {value}");
            }
        }

        protected async Task<SavesMap> ScanLocalFilesAsync(string filter = null)
        {
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

        protected void PrintScanErrors(bool details, SavesMap map)
        {
            PrintScanErrors(details, map.Scripts.Cast<LocalFile>().Concat(map.Scenes).ToArray());
        }

        protected void PrintScanErrors(bool details, params LocalFile[] files)
        {
            var logs = files?.Where(f => f.Errors != null && f.Errors.Count > 0).SelectMany(f => f.Errors?.Select(e => (f, e))).ToList();
            if (logs == null || logs.Count == 0) return;

            var grouped = logs.GroupBy(fe => fe.e.Level).ToDictionary(g => g.Key, g => g.ToArray());
            if (!grouped.TryGetValue(LocalFileErrorLevel.Error, out var errors)) errors = new (LocalFile f, LocalFileError e)[0];
            if (!grouped.TryGetValue(LocalFileErrorLevel.Warning, out var warnings)) warnings = new (LocalFile f, LocalFileError e)[0];

            if (details)
            {
                if (errors.Length > 0)
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

                if (warnings.Length > 0)
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
            else if (errors.Length > 0 || warnings.Length > 0)
            {
                using (Renderer.WithColor(errors.Length > 0 ? ConsoleColor.Red : ConsoleColor.Yellow))
                {
                    Renderer.Error.WriteLine($"Found {Pluralize(warnings?.Length ?? 0, "warning", "warnings")} and {Pluralize(errors?.Length ?? 0, "error", "errors")} while scanning. Run with --errors to print them.");
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
                if (file.Reason != null)
                    Renderer.Write($" {file.Reason}", ConsoleColor.DarkGray);
                Renderer.WriteLine();
            }
        }

        protected void PrintScriptToPackage(RegistrySavesMatch match, RegistryPackageVersion latestCompatVersion, RegistryPackageVersion latestVersion)
        {
            PrintScriptToPackage(match.Remote, latestCompatVersion, latestVersion, match.Local);
        }

        protected void PrintScriptToPackage(RegistryPackageFileContext context, RegistryPackageVersion latestCompatVersion, RegistryPackageVersion latestVersion, LocalScriptFile local)
        {
            var (_, package, version) = context;
            Renderer.Write($"Script ");
            Renderer.Write(Controller.GetDisplayPath(local.FullPath), ConsoleColor.Blue);
            Renderer.Write($" is ");
            Renderer.Write($"{package.Name} v{version.Version}", ConsoleColor.Cyan);
            Renderer.Write($" > ");
            if (latestCompatVersion == null && latestVersion == null)
            {
                Renderer.Write($"  already up to date", ConsoleColor.DarkGray);
                Renderer.WriteLine();
            }
            else if (latestCompatVersion != null && latestVersion == latestCompatVersion)
            {
                Renderer.Write($"  new version available: v{latestCompatVersion.Version}", ConsoleColor.Magenta);
                Renderer.WriteLine();
                Renderer.WriteLine($"  Released {latestCompatVersion.Created.ToLocalTime().ToString("D")}: {latestCompatVersion.Notes ?? "No release notes"}");
            }
            else if (latestCompatVersion != null && latestVersion != latestCompatVersion)
            {
                Renderer.Write($"  new compatible version available: v{latestCompatVersion.Version}, also (v{latestVersion.Version} is out!)", ConsoleColor.Magenta);
                Renderer.WriteLine();
                Renderer.WriteLine($"  Released {latestCompatVersion.Created.ToLocalTime().ToString("D")}: {latestCompatVersion.Notes ?? "No release notes"}");
            }
            else if (latestVersion != null)
            {
                Renderer.Write($"  new version available: v{latestVersion.Version} (might be incompatible)", ConsoleColor.Red);
                Renderer.WriteLine();
                Renderer.WriteLine($"  Released {latestCompatVersion.Created.ToLocalTime().ToString("D")}: {latestCompatVersion.Notes ?? "No release notes"}");
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

        private void WriteLogs(T args)
        {
            Renderer.WriteLine($"===== LOGS ({args.Log}) =====", ConsoleColor.White);
            while (Logger.Dequeue(out var log))
            {
                switch (log.Level)
                {
                    case LogLevel.Verbose:
                        Renderer.WriteLine(log.Message, ConsoleColor.DarkGray);
                        break;
                    case LogLevel.Info:
                        Renderer.WriteLine(log.Message, ConsoleColor.Gray);
                        break;
                    case LogLevel.Warning:
                        Renderer.WriteLine(log.Message, ConsoleColor.Yellow);
                        break;
                    case LogLevel.Error:
                        Renderer.WriteLine(log.Message, ConsoleColor.Red);
                        break;
                    default:
                        Renderer.WriteLine(log.Message, ConsoleColor.DarkMagenta);
                        break;
                }
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

    public abstract class CommonArguments
    {
        public DirectoryInfo VaM { get; set; }
        public bool Force { get; set; }
        public LogLevel Log { get; set; }
    }
}
