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
            var flattened = registry.FlattenFiles(RegistryPackageType.Scripts).Where(IsScript).ToList();
            var scripts = new List<LocalScriptFile>(saves.Scripts);

            var withHash = flattened
                .Where(svf => svf.File.Hash?.Value != null)
                // NOTE: This will match the first of two packages sharing the same file
                .GroupBy(svf => svf.File.Hash.Value)
                .ToDictionary(svf => svf.Key, svf => svf.ToList());
            var byHash = MatchByHash(scripts, withHash).ToArray();
            var matchedByHash = new HashSet<LocalScriptFile>(byHash.Select(m => m.Local));

            scripts = scripts.Where(s => !matchedByHash.Contains(s)).ToList();
            var withFilename = flattened
                .Where(remote => remote.File.Filename != null)
                .GroupBy(remote => Path.GetFileName(remote.File.Filename))
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.ToList());
            var byFilename = MatchByFilename(scripts, withFilename).ToArray();
            var matchedByFilename = new HashSet<LocalScriptFile>(byFilename.Concat(byHash).Select(m => m.Local));

            return new RegistrySavesMatches
            {
                HashMatches = byHash,
                FilenameMatches = byFilename,
                NoMatches = scripts.Where(s => !matchedByFilename.Contains(s)).ToArray()
            };
        }

        private bool IsScript(RegistryPackageFileContext ctx)
        {
            var extension = Path.GetExtension(ctx.File.Filename);
            return extension == ".cs" || extension == ".cslist";
        }

        private IEnumerable<RegistrySavesMatch> MatchByHash(IEnumerable<LocalScriptFile> scripts, Dictionary<string, List<RegistryPackageFileContext>> byHash)
        {
            foreach (var script in scripts)
            {
                if (script.Hash == null || !byHash.TryGetValue(script.Hash, out var remotes))
                    continue;

                if (script is LocalScriptListFile scriptList)
                {
                    foreach (var remote in remotes)
                    {
                        if (scriptList.Scripts.All(s => remote.Version.Files.Any(f => s.FileName != null && s.Hash == f.Hash?.Value)))
                            yield return new RegistrySavesMatch(remote, script);
                    }
                    continue;
                }

                yield return new RegistrySavesMatch(remotes.First(), script);
            }
        }

        private IEnumerable<RegistrySavesMatch> MatchByFilename(IEnumerable<LocalScriptFile> localFiles, Dictionary<string, List<RegistryPackageFileContext>> byFilename)
        {
            foreach (var local in localFiles)
            {
                if (!byFilename.TryGetValue(local.FileName, out var remotes))
                    continue;

                if (local is LocalScriptListFile scriptList)
                {
                    foreach (var remote in remotes)
                    {
                        if (scriptList.Scripts.All(s => remote.Version.Files.Any(f => s.FileName == Path.GetFileName(f.Filename))))
                            yield return new RegistrySavesMatch(remote, local);
                    }
                    continue;
                }

                var packages = remotes.GroupBy(r => r.Package.Name);
                var package = packages.SingleOrDefault();
                yield return new RegistrySavesMatch(package.FirstOrDefault(), local);
            }
        }
    }
}
