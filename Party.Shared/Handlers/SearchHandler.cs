using System.Collections.Generic;
using System.Linq;
using Party.Shared.Discovery;
using Party.Shared.Registry;
using Party.Shared.Resources;
using Party.Shared.Results;

namespace Party.Shared.Handlers
{
    public class SearchHandler
    {
        private readonly PartyConfiguration _config;

        public SearchHandler(PartyConfiguration config)
        {
            _config = config;
        }

        public IEnumerable<SearchResult> Execute(Registry.Registry registry, SavesMap saves, string query, bool showUsage)
        {
            foreach (var package in registry.Scripts)
            {
                if (!string.IsNullOrEmpty(query))
                {
                    if (!MatchesQuery(package, query))
                    {
                        continue;
                    }
                }
                var trusted = package.Versions.SelectMany(v => v.Files).All(f => _config.Registry.TrustedDomains.Any(t => f.Url.StartsWith(t)));
                Scene[] scenes = null;
                if (showUsage)
                {
                    var scripts = package.Versions?.SelectMany(v => v.Files ?? new List<RegistryFile>()).Select(f => f.GetIdentifier());
                    scenes = scripts?.Select(s => saves.ScriptMaps.GetValueOrDefault(s)).Where(r => r != null && r.Scenes != null).SelectMany(r => r.Scenes).Distinct().ToArray();
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
