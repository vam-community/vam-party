using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.Shared.Discovery;

namespace Party.CLI.Commands
{
    public class ListScriptsCommand
    {
        [Verb("scripts", HelpText = "Show all scripts")]
        public class Options : CommonOptions
        {
            [Option("online", Default = false, HelpText = "Include online scripts from your registries")]
            public bool Online { get; set; }

            [Option("scenes", Default = false, HelpText = "Show scenes in which scripts are used")]
            public bool Scenes { get; set; }
        }

        public static async Task<int> ExecuteAsync(Options opts, IConfiguration config)
        {
            var savesDirectory = Path.GetFullPath(opts.Saves ?? Path.Combine(Environment.CurrentDirectory, "Saves"));
            var ignore = config.GetSection("Scanning:Ignore").GetChildren().Select(x => x.Value).ToArray();
            var map = await SavesResolver.Resolve(SavesScanner.Scan(savesDirectory, ignore));

            Console.WriteLine("Scripts:");
            foreach (var scriptMap in map.ScriptMaps.OrderBy(sm => sm.Key))
            {
                Console.WriteLine($"- {scriptMap.Value.Name} ({Pluralize(scriptMap.Value.Scripts.Count(), "copy", "copies")} used by {Pluralize(scriptMap.Value.Scenes.Count(), "scene", "scenes")})");

                if (opts.Scenes)
                {
                    foreach (var scene in scriptMap.Value.Scenes)
                    {
                        Console.WriteLine($"  - {scene.Location.RelativePath}");
                    }
                }
            }

            return 0;
        }

        private static string Pluralize(int count, string singular, string plural)
        {
            if (count == 1)
                return $"{count} {singular}";
            else
                return $"{count} {plural}";
        }
    }
}
