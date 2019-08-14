using System.Collections.Generic;
using System.Linq;
using Party.Shared.Resources;
using Party.Shared.Results;

namespace Party.Shared.Handlers
{
    public class SearchHandler
    {
        private readonly PartyConfiguration _config;

        public SearchHandler(PartyConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public IEnumerable<SearchResult> SearchAsync(RegistryResult registry, SavesMapResult saves, string query, bool showUsage)
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
                Script[] scripts = null;
                Scene[] scenes = null;
                if (showUsage)
                {
                    var allVersionsIdentifiers = package.Versions?.SelectMany(v => v.Files ?? new List<RegistryResult.RegistryFile>()).Select(f => f.GetIdentifier());
                    scripts = allVersionsIdentifiers.Select(id => saves.IdentifierScriptMap.GetValueOrDefault(id)).Where(s => s != null).Distinct().ToArray();
                    scenes = scripts.SelectMany(s => s.Scenes).Distinct().ToArray();
                }
                yield return new SearchResult
                {
                    Package = package,
                    Trusted = trusted,
                    Scripts = scripts,
                    Scenes = scenes
                };
            }
        }

        private bool MatchesQuery(RegistryResult.RegistryScript package, string query)
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
