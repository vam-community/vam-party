using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Discovery;
using Party.Shared.Registry;
using Party.Shared.Resources;

namespace Party.Shared.Commands
{
    public class SearchPackagesHandler : HandlerBase
    {
        public class SearchResult
        {
            public bool Trusted { get; internal set; }
            public RegistryScript Script { get; internal set; }
            public Scene[] Scenes { get; internal set; }
        }

        public SearchPackagesHandler(PartyConfiguration config)
        : base(config)
        {
        }

        public async IAsyncEnumerable<SearchResult> ExecuteAsync(string query, bool showUsage)
        {
            var client = new RegistryLoader(Config.Registry.Urls);
            Registry.Registry registry = null;
            SavesMap map = null;
            await Task.WhenAll(
                ((Func<Task>)(async () => { registry = await client.Acquire().ConfigureAwait(false); }))(),
                ((Func<Task>)(async () => { map = showUsage ? await ScanLocalScripts().ConfigureAwait(false) : null; }))()
            ).ConfigureAwait(false);
            foreach (var package in registry.Scripts)
            {
                if (!string.IsNullOrEmpty(query))
                {
                    if (!MatchesQuery(package, query))
                    {
                        continue;
                    }
                }
                var trusted = package.Versions.SelectMany(v => v.Files).All(f => Config.Registry.TrustedDomains.Any(t => f.Url.StartsWith(t)));
                Scene[] scenes = null;
                if (showUsage)
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

        private bool MatchesQuery(RegistryScript package, string query)
        {
            if (package.Name.Contains(query))
            {
                return true;
            }
            if (package.Author?.Name?.Contains(query) ?? false)
            {
                return true;
            }
            if (package.Description?.Contains(query) ?? false)
            {
                return true;
            }
            if (package.Tags?.Any(tag => tag.Contains(query)) ?? false)
            {
                return true;
            }
            return false;
        }
    }
}
