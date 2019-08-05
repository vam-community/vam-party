using System;
using System.IO;
using CommandLine;
using Party.Shared;

namespace Party.CLI
{
    class Program
    {
        [Verb("scenes", HelpText = "Show all scenes")]
        class ScenesOptions
        {
            [Option('p', "path", Required = false, HelpText = "The Saves directory; defaults to the Saves folder under the current directory.")]
            public string Saves { get; set; }
        }

        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<ScenesOptions>(args)
              .MapResult(
                (ScenesOptions opts) => RunScenes(opts),
                errs => 1);
        }

        private static int RunScenes(ScenesOptions opts)
        {
            var savesDirectory = Path.GetFullPath(opts.Saves ?? Path.Combine(Environment.CurrentDirectory, "Saves"));
            var saves = SavesScanner.Scan(savesDirectory);

            Console.WriteLine("Scenes:");
            foreach (var scene in saves.Scenes)
            {
                Console.WriteLine($"- {scene.Filename}");
            }

            Console.WriteLine("Scripts:");
            foreach (var script in saves.Scripts)
            {
                Console.WriteLine($"- {script.Filename} ({script.GetHash()})");
            }

            return 0;
        }
    }
}
