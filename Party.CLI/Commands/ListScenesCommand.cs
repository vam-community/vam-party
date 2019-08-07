using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.Shared.Discovery;
using Party.Shared.Resources;

namespace Party.CLI.Commands
{
    public class ListScenesCommand
    {
        [Verb("scenes", HelpText = "Show all scenes")]
        public class Options : CommonOptions
        {
            [Option("scripts", Default = false, HelpText = "Whether to show scripts used in each scene")]
            public bool Scripts { get; set; }
        }

        public static async Task<int> ExecuteAsync(Options opts, IConfiguration config)
        {
            var savesDirectory = Path.GetFullPath(opts.Saves ?? Path.Combine(Environment.CurrentDirectory, "Saves"));
            var ignore = config.GetSection("Scanning:Ignore").GetChildren().Select(x => x.Value).ToArray();
            var scenes = SavesScanner.Scan(savesDirectory, ignore).OfType<Scene>();

            Console.WriteLine("Scenes:");
            foreach (var scene in scenes)
            {
                Console.WriteLine($"- {scene.Location.RelativePath}");

                if (opts.Scripts)
                {
                    await foreach (var script in scene.GetScriptsAsync())
                    {
                        Console.WriteLine($"  - {script.Location.RelativePath} ({script.GetHash()})");
                    }
                }
            }

            return 0;
        }
    }
}
