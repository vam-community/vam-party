using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using System.Threading.Tasks;
using Party.Shared;
using Party.Shared.Models;

namespace Party.CLI.Commands
{
    public abstract class CommandBase
    {
        protected readonly IRenderer Renderer;
        protected readonly PartyConfiguration Config;
        protected readonly IPartyController Controller;

        protected CommandBase(IRenderer renderer, PartyConfiguration config, DirectoryInfo saves, IPartyController controller)
        {
            Renderer = renderer;
            Config = GetConfig(config, saves);
            Controller = controller;
        }

        private static PartyConfiguration GetConfig(PartyConfiguration config, DirectoryInfo saves)
        {
            if (saves != null)
            {
                config.VirtAMate.SavesDirectory = Path.GetFullPath(saves.FullName, Environment.CurrentDirectory);
            }
            return config;
        }

        protected static void AddCommonOptions(Command command)
        {
            command.AddOption(new Option("--saves", "Specify the Saves folder to use") { Argument = new Argument<DirectoryInfo>().ExistingOnly() });
        }

        protected async Task<(SavesMap, Registry)> GetSavesAndRegistryAsync()
        {
            var registryTask = Controller.GetRegistryAsync();
            var savesTask = Controller.GetSavesAsync();

            await Task.WhenAll();

            var registry = await registryTask;
            var saves = await savesTask;
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

        protected static string Pluralize(int count, string singular, string plural)
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
