using System.Collections.Generic;
using System.IO;
using System.Linq;
using Party.Shared.Models;

namespace Party.Shared.Handlers
{
    public class RegistrySavesMatchHandler
    {
        public RegistrySavesMatches Match(SavesMap saves, Registry registry)
        {
            var flattened = registry.FlattenFiles().ToList();
            var scripts = new List<Script>(saves.Scripts);

            var withHash = flattened
                .Where(svf => svf.file.Hash?.Value != null)
                .GroupBy(svf => svf.file.Hash.Value)
                .ToDictionary(svf => svf.Key, svf => svf.First());
            var byHash = MatchByHash(scripts, withHash).ToArray();
            var matchedByHash = new HashSet<Script>(byHash.Select(m => m.Local));

            scripts = scripts.Where(s => !matchedByHash.Contains(s)).ToList();
            var withFilename = flattened
                .Where(svf => svf.file.Filename != null)
                .GroupBy(svf => Path.GetFileName(svf.file.Filename))
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());
            var byFilename = MatchByFilename(scripts, withFilename).ToArray();
            var matchedByFilename = new HashSet<Script>(byFilename.Concat(byHash).Select(m => m.Local));

            return new RegistrySavesMatches
            {
                HashMatches = byHash,
                FilenameMatches = byFilename,
                NoMatches = scripts.Where(s => !matchedByFilename.Contains(s)).ToArray()
            };
        }

        private IEnumerable<RegistrySavesMatch> MatchByHash(IEnumerable<Script> scripts, Dictionary<string, (RegistryPackage script, RegistryPackageVersion version, RegistryFile file)> byHash)
        {
            foreach (var script in scripts)
            {
                if (!byHash.TryGetValue(script.Hash, out var svf))
                {
                    continue;
                }

                if (script is ScriptList scriptList)
                {
                    if (!scriptList.Scripts.All(s => svf.version.Files.Any(f => s.Hash == f.Hash.Value)))
                        continue;
                }

                yield return new RegistrySavesMatch(svf, script);
            }
        }

        private IEnumerable<RegistrySavesMatch> MatchByFilename(IEnumerable<Script> scripts, Dictionary<string, (RegistryPackage script, RegistryPackageVersion version, RegistryFile file)> byFilename)
        {
            foreach (var script in scripts)
            {
                if (script is ScriptList)
                {
                    // We'll only match simple cases
                    continue;
                }

                if (!byFilename.TryGetValue(script.FileName, out var svf))
                {
                    continue;
                }

                yield return new RegistrySavesMatch(svf, script);
            }
        }
    }
}
