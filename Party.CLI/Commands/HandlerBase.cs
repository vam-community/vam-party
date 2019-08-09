using System;
using System.IO;
using CommandLine;
using Party.Shared.Commands;

namespace Party.CLI.Commands
{
    public abstract class HandlerBase
    {
        protected PartyConfiguration Config;

        protected HandlerBase(PartyConfiguration config)
        {
            Config = config;
        }

        public abstract class CommonOptions
        {
            [Option('s', "saves", Required = false, HelpText = "The Saves directory; defaults to the Saves folder under the current directory.")]
            public string SavesFolder { get; set; }
        }

        protected static PartyConfiguration GetConfig(CommonOptions opts, PartyConfiguration config)
        {
            if (!string.IsNullOrEmpty(opts.SavesFolder))
                config.VirtAMate.SavesDirectory = Path.GetFullPath(opts.SavesFolder, Environment.CurrentDirectory);
            return config;
        }
    }
}