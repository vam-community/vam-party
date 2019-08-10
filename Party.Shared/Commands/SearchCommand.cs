using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Discovery;
using Party.Shared.Registry;
using Party.Shared.Resources;

namespace Party.Shared.Commands
{
    public class SearchCommand : CommandBase
    {
        public class SearchResult
        {
            public bool Trusted { get; internal set; }
            public RegistryScript Script { get; internal set; }
            public Scene[] Scenes { get; internal set; }
        }

        public SearchCommand(PartyConfiguration config)
        : base(config)
        {
        }

        public async IAsyncEnumerable<SearchResult> ExecuteAsync(string filter, bool local)
        {
            var client = new RegistryLoader(Config.Registry.Urls);
            Registry.Registry registry = null;
            SavesMap map = null;
            await Task.WhenAll(
                ((Func<Task>)(async () => { registry = await client.Acquire().ConfigureAwait(false); }))(),
                ((Func<Task>)(async () => { map = local ? await ScanLocalScripts().ConfigureAwait(false) : null; }))()
            ).ConfigureAwait(false);
            foreach (var package in registry.Scripts)
            {
                var trusted = package.Versions.SelectMany(v => v.Files).All(f => Config.Registry.TrustedDomains.Any(t => f.Url.StartsWith(t)));
                Scene[] scenes = null;
                if (local)
                {
                    var scripts = package.Versions?.SelectMany(v => v.Files ?? new List<RegistryFile>()).Select(f => f.GetIdentifier());
                    scenes = scripts?.Select(s => map.ScriptMaps.GetValueOrDefault(s)).Where(r => r != null && r.Scenes != null).SelectMany(r => r.Scenes).Distinct().ToArray();
                }
                yield return new SearchResult
                {
                    Script = package,
                    Trusted = trusted,
                    Scenes = scenes
                };
            }
        }
    }
}
