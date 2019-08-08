using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Party.Shared.Discovery;
using Party.Shared.Registry;

namespace Party.CLI.Commands
{
    public class SearchScriptsCommand
    {
        [Verb("search", HelpText = "Search remote scripts from the registry and locally")]
        public class Options : CommonOptions
        {
        }

        public static async Task<int> ExecuteAsync(Options opts, IConfiguration config)
        {
            // TODO: Extension on IConfiguration
            var trustedDomains = config.GetSection("Registry:TrustedDomains").GetChildren().Select(x => x.Value);
            var client = new RegistryLoader(config.GetSection("Registry:Urls").GetChildren().Select(x => x.Value).ToArray());
            var registry = await client.Acquire();
            foreach (var script in registry.Scripts)
            {
                var trusted = script.Versions.SelectMany(v => v.Files).All(f => trustedDomains.Any(t => f.Url.StartsWith(t)));
                var trustedMsg = trusted ? "" : " [NOT TRUSTED]";
                Console.WriteLine($"- {script.Name} by {script.Author.Name} (v{script.GetLatestVersion().Version}){trustedMsg}");
            }
            /*
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
            */

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
