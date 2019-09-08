using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;
using Party.Shared.Resources;

namespace Party.CLI.Commands
{
    public abstract class CommandBase
    {
        private static PartyConfiguration GetConfig(PartyConfiguration config, DirectoryInfo vam)
        {
            if (vam != null)
            {
                config.VirtAMate.VirtAMateInstallFolder = Path.GetFullPath(vam.FullName, Environment.CurrentDirectory);
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

        protected async Task<(SavesMap, Registry)> GetSavesAndRegistryAsync(string filter = null)
        {
            // NOTE: When specifying --noop to status, it puts --noop in a filter, and returns nothing. Try to avoid that, or at least specify why nothing has been returned?

            var filterPackage = filter != null && filter.IndexOf(".") == -1;
            var filterPath = filter != null && !filterPackage;

            using var registryTask = Controller.GetRegistryAsync();
            // TODO: If the item is a package (no extension), resolve it to a path (if the plugin was not downloaded, throw)
            // TODO: When the filter is a scene, mark every script that was not referenced by that scene as not safe for cleanup; also remove them for display
            using var savesTask = Controller.GetSavesAsync(filterPath ? Path.GetFullPath(filter) : null);

            await Task.WhenAll();

            var registry = await registryTask;
            var saves = await savesTask;

            // TODO: Put in controller
            if (filterPackage)
            {
                var packageHashes = new HashSet<string>(registry.Scripts.Where(s => filter.Equals(s.Name, StringComparison.InvariantCultureIgnoreCase)).SelectMany(s => s.Versions).SelectMany(v => v.Files).Select(f => f.Hash.Value).Distinct());
                saves.Scripts = saves.Scripts.Where(s =>
                {
                    if (s is ScriptList scriptList)
                        return new[] { scriptList.Hash }.Concat(scriptList.Scripts.Select(c => c.Hash)).All(h => packageHashes.Contains(h));
                    else
                        return packageHashes.Contains(s.Hash);
                }).ToArray();
            }

            return (saves, registry);
        }

        protected void PrintWarnings(bool details, SavesError[] errors)
        {
            if (errors == null || errors.Length == 0) return;

            if (details)
            {
                using (Renderer.WithColor(ConsoleColor.Yellow))
                {
                    Renderer.WriteLine("Scene warnings:");
                    foreach (var error in errors)
                    {
                        Renderer.Error.Write("  ");
                        Renderer.Error.WriteLine($"{Controller.GetDisplayPath(error.File)}: {error.Error}");
                    }
                }
                Renderer.WriteLine();
            }
            else
            {
                using (Renderer.WithColor(ConsoleColor.Yellow))
                {
                    Renderer.Error.WriteLine($"There were {errors.Length} errors in the saves folder. Run with --warnings to print them.");
                }
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
    }
}
