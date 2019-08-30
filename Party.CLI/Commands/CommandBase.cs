using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using Party.Shared;

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

        protected void PrintWarnings(string[] errors)
        {
            if (errors == null || errors.Length == 0) return;

            using (Renderer.WithColor(ConsoleColor.Yellow))
            {
                foreach (var error in errors)
                {
                    Renderer.Error.WriteLine(error);
                }
            }
        }

        protected void PrintWarningsCount(string[] errors)
        {
            if (errors == null || errors.Length == 0) return;

            using (Renderer.WithColor(ConsoleColor.Yellow))
            {
                Renderer.Error.WriteLine($"There were {errors.Length} errors in the saves folder. Run with --warnings to print them.");
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
