using System.Collections.Generic;
using System.IO;
using System.Linq;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.Shared.Handlers
{
    public class MatchLocalFilesToRegistryHandler
    {
        public RegistrySavesMatches MatchLocalFilesToRegistry(SavesMap saves, Registry registry)
        {
            // TODO: Should handle other types
            var flattened = registry.FlattenFiles(RegistryPackageType.Scripts).ToList();
            var scripts = new List<LocalScriptFile>(saves.Scripts);

            var withHash = flattened
                .Where(svf => svf.File.Hash?.Value != null)
                .GroupBy(svf => svf.File.Hash.Value)
                .ToDictionary(svf => svf.Key, svf => svf.First());
            var byHash = MatchByHash(scripts, withHash).ToArray();
            var matchedByHash = new HashSet<LocalScriptFile>(byHash.Select(m => m.Local));

            scripts = scripts.Where(s => !matchedByHash.Contains(s)).ToList();
            var withFilename = flattened
                .Where(remote => remote.File.Filename != null)
                .GroupBy(remote => Path.GetFileName(remote.File.Filename))
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());
            var byFilename = MatchByFilename(scripts, withFilename).ToArray();
            var matchedByFilename = new HashSet<LocalScriptFile>(byFilename.Concat(byHash).Select(m => m.Local));

            return new RegistrySavesMatches
            {
                HashMatches = byHash,
                FilenameMatches = byFilename,
                NoMatches = scripts.Where(s => !matchedByFilename.Contains(s)).ToArray()
            };
        }

        private IEnumerable<RegistrySavesMatch> MatchByHash(IEnumerable<LocalScriptFile> scripts, Dictionary<string, RegistryPackageFileContext> byHash)
        {
            foreach (var script in scripts)
            {
                if (script.Hash == null || !byHash.TryGetValue(script.Hash, out var remote))
                    continue;

                if (script is LocalScriptListFile scriptList)
                {
                    if (!scriptList.Scripts.All(s => remote.Version.Files.Any(f => s.FileName != null && s.Hash == f.Hash?.Value)))
                        continue;
                }

                yield return new RegistrySavesMatch(remote, script);
            }
        }

        private IEnumerable<RegistrySavesMatch> MatchByFilename(IEnumerable<LocalScriptFile> localFiles, Dictionary<string, RegistryPackageFileContext> byFilename)
        {
            foreach (var local in localFiles)
            {
                if (local is LocalScriptListFile)
                {
                    // We'll only match simple cases
                    continue;
                }

                if (!byFilename.TryGetValue(local.FileName, out var remote))
                {
                    continue;
                }

                yield return new RegistrySavesMatch(remote, local);
            }
        }
    }
}
