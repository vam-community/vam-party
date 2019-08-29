using System;
using System.Collections.Generic;
using System.Linq;
using Party.Shared.Resources;
using Party.Shared.Models;

namespace Party.Shared.Handlers
{
    public class SearchHandler
    {
        private readonly PartyConfiguration _config;

        public SearchHandler(PartyConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public IEnumerable<SearchResult> Search(Registry registry, SavesMap saves, string query, bool showUsage)
        {
            if (registry is null) throw new ArgumentNullException(nameof(registry));
            if (registry?.Scripts is null) throw new ArgumentException("registry does not have any scripts", nameof(registry));

            foreach (var package in registry.Scripts)
            {
                if (!string.IsNullOrEmpty(query))
                {
                    if (!MatchesQuery(package, query))
                    {
                        continue;
                    }
                }
                var trusted = package.Versions?.SelectMany(v => v.Files).All(f => _config.Registry.TrustedDomains.Any(t => f.Url.StartsWith(t))) ?? false;
                Script[] scripts = null;
                Scene[] scenes = null;
                if (showUsage)
                {
                    // TODO: We should consider all files from a specific version of plugin together
                    var allFilesFromAllVersions = package.Versions?.SelectMany(v => v.Files ?? new List<RegistryFile>());
                    scripts = allFilesFromAllVersions.SelectMany(regFile => saves.ScriptsByFilename.Values.Where(saveFile => saveFile.Hash == regFile.Hash.Value)).Distinct().ToArray();
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

        private bool MatchesQuery(RegistryScript package, string query)
        {
            if (package.Name?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                return true;
            }
            if (package.Author?.Name?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                return true;
            }
            if (package.Description?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                return true;
            }
            if (package.Tags?.Any(tag => tag.Contains(query, StringComparison.InvariantCultureIgnoreCase)) ?? false)
            {
                return true;
            }
            return false;
        }
    }
}
