using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Party.Shared.Commands;
using Party.Shared.Discovery;
using Party.Shared.Resources;

namespace Party.CLI.Commands
{
    public class ListScenesCommand : HandlerBase
    {
        [Verb("scenes", HelpText = "Show all scenes")]
        public class Options : HandlerBase.CommonOptions
        {
            [Option("scripts", Default = false, HelpText = "Whether to show scripts used in each scene")]
            public bool Scripts { get; set; }
        }

        public ListScenesCommand(PartyConfiguration config) : base(config)
        {
        }

        public async Task<int> ExecuteAsync(Options opts)
        {
            var config = GetConfig(opts, Config);
            var savesDirectory = config.VirtAMate.SavesDirectory;
            var ignore = config.Scanning.Ignore;
            var scenes = SavesScanner.Scan(savesDirectory, ignore).OfType<Scene>();

            Console.WriteLine("Scenes:");
            foreach (var scene in scenes)
            {
                Console.WriteLine($"- {scene.Location.RelativePath}");

                if (opts.Scripts)
                {
                    await foreach (var script in scene.GetScriptsAsync())
                    {
                        Console.WriteLine($"  - {script.Location.RelativePath} ({script.GetHashAsync()})");
                    }
                }
            }

            return 0;
        }
    }
}
