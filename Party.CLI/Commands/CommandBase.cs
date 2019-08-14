﻿using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using Party.Shared;

namespace Party.CLI.Commands
{
    public abstract class CommandBase
    {
        protected readonly IRenderer Output;
        protected readonly PartyConfiguration Config;
        protected readonly PartyController Controller;

        protected CommandBase(IRenderer output, PartyConfiguration config, string saves, PartyController controller)
        {
            Output = output;
            Config = GetConfig(config, saves);
            Controller = controller;
        }

        private static PartyConfiguration GetConfig(PartyConfiguration config, string saves)
        {
            if (!string.IsNullOrEmpty(saves))
                config.VirtAMate.SavesDirectory = Path.GetFullPath(saves, Environment.CurrentDirectory);
            return config;
        }

        protected static void AddCommonOptions(Command command)
        {
            command.AddOption(new Option("--saves", "Specify the Saves folder to use") { Argument = new Argument<DirectoryInfo>().ExistingOnly() });
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