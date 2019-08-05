using System;
using System.IO;
using CommandLine;
using Party.Shared;

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

        public static int Execute(Options opts)
        {
            var savesDirectory = Path.GetFullPath(opts.Saves ?? Path.Combine(Environment.CurrentDirectory, "Saves"));
            var saves = SavesScanner.Scan(savesDirectory);

            Console.WriteLine("Scenes:");
            foreach (var scene in saves.Scenes)
            {
                Console.WriteLine($"- {scene.Location.RelativePath}");

                if (opts.Scripts)
                {
                    foreach (var script in saves.Scripts)
                    {
                        Console.WriteLine($"  - {script.Location.RelativePath} ({script.GetHash()})");
                    }
                }
            }

            return 0;
        }
    }
}
