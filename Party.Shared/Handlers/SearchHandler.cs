using System;
using System.Collections.Generic;
using System.Linq;
using Party.Shared.Models;
using Party.Shared.Models.Registries;

namespace Party.Shared.Handlers
{
    public class SearchHandler
    {
        private readonly string[] _trustedDomains;

        public SearchHandler(string[] trustedDomains)
        {
            _trustedDomains = trustedDomains ?? throw new ArgumentNullException(nameof(trustedDomains));
        }

        public IEnumerable<SearchResult> Search(Registry registry, string query)
        {
            if (registry is null) throw new ArgumentNullException(nameof(registry));
            if (registry?.Packages is null) throw new ArgumentException("registry does not have any scripts", nameof(registry));

            // TODO: Search in all package types
            foreach (var package in registry.Packages.Scripts)
            {
                if (!string.IsNullOrEmpty(query))
                {
                    if (!MatchesQuery(package, query))
                    {
                        continue;
                    }
                }
                var trusted = package.Versions?
                    .SelectMany(v => v.Files)
                    .Where(f => f.Url != null && !f.Ignore)
                    .All(f => _trustedDomains.Any(t => f.Url.StartsWith(t)))
                    ?? false;
                yield return new SearchResult
                {
                    Package = package,
                    Trusted = trusted,
                };
            }
        }

        private bool MatchesQuery(RegistryPackage package, string query)
        {
            if ((package.Name?.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) ?? -1) > -1)
            {
                return true;
            }
            if ((package.Author?.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) ?? -1) > -1)
            {
                return true;
            }
            if ((package.Description?.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) ?? -1) > -1)
            {
                return true;
            }
            if (package.Tags?.Any(tag => tag.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) > -1) ?? false)
            {
                return true;
            }
            return false;
        }
    }
}
