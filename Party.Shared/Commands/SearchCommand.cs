using System.Collections.Generic;
using System.Linq;
using Party.Shared.Registry;

namespace Party.Shared.Commands
{
    public class SearchCommand : CommandBase
    {
        public class SearchResult
        {
            public bool Trusted { get; set; }
            public RegistryScript Script { get; set; }
        }

        public SearchCommand(PartyConfiguration config)
        : base(config)
        {
        }

        public async IAsyncEnumerable<SearchResult> ExecuteAsync()
        {
            // TODO: Extension on IConfiguration
            var client = new RegistryLoader(_config.Registry.Urls);
            var registry = await client.Acquire();
            foreach (var script in registry.Scripts)
            {
                var trusted = script.Versions.SelectMany(v => v.Files).All(f => _config.Registry.TrustedDomains.Any(t => f.Url.StartsWith(t)));
                yield return new SearchResult
                {
                    Script = script,
                    Trusted = trusted
                };
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
        }
    }
}
