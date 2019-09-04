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

        protected CommandBase(IConsoleRenderer renderer, PartyConfiguration config, DirectoryInfo vam, IPartyController controller)
        {
            Renderer = renderer;
            Config = GetConfig(config, vam);
            Controller = controller;
        }

        protected static void AddCommonOptions(Command command)
        {
            command.AddOption(new Option("--vam", "Specify the Virt-A-Mate install folder") { Argument = new Argument<DirectoryInfo>().ExistingOnly() });
        }

        public abstract class CommonArguments
        {
            public DirectoryInfo VaM { get; set; }
        }

        protected async Task<(SavesMap, Registry)> GetSavesAndRegistryAsync(string[] filters = null)
        {
            var filterPackages = filters?.Where(f => f.IndexOf(".") == -1).ToArray();
            var filterPaths = filters?.Where(f => !filterPackages.Contains(f)).ToArray();

            var registryTask = Controller.GetRegistryAsync();
            // TODO: If the item is a package (no extension), resolve it to a path (if the plugin was not downloaded, throw)
            var savesTask = Controller.GetSavesAsync(filterPaths?.Select(Path.GetFullPath).ToArray());

            await Task.WhenAll();

            var registry = await registryTask;
            var saves = await savesTask;

            if (filterPackages != null && filterPackages.Length > 0)
            {
                var packageHashes = new HashSet<string>(registry.Scripts.Where(s => filterPackages.Contains(s.Name)).SelectMany(s => s.Versions).SelectMany(v => v.Files).Select(f => f.Hash.Value).Distinct());
                saves.ScriptsByFilename = saves.ScriptsByFilename.Where(s =>
                {
                    if (s.Value is ScriptList scriptList)
                        return new[] { scriptList.Hash }.Concat(scriptList.Scripts.Select(c => c.Hash)).All(h => packageHashes.Contains(h));
                    else
                        return packageHashes.Contains(s.Value.Hash);
                }).ToDictionary(s => s.Key, s => s.Value);
            }

            return (saves, registry);
        }

        protected void PrintWarnings(bool details, string[] errors)
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
                        Renderer.Error.WriteLine(error);
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
