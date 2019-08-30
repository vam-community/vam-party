using System;
using System.Collections.Generic;
using System.Linq;
using Party.Shared.Models;
using Party.Shared.Resources;

namespace Party.Shared.Handlers
{
    public class RegistrySavesMatchHandler
    {
        public RegistrySavesMatch[] Match(SavesMap saves, Registry registry)
        {
            var registryVersionsByHash = registry.Scripts
                .SelectMany(s => s.Versions.Select(v => (s, v)))
                .SelectMany(sv => sv.v.Files.Select(f => (script: sv.s, version: sv.v, file: f)))
                .ToDictionary(svf => svf.file.Hash.Value, svf => svf);

            return Match(saves, registryVersionsByHash).ToArray();
        }

        public IEnumerable<RegistrySavesMatch> Match(SavesMap saves, Dictionary<string, (RegistryScript script, RegistryScriptVersion version, RegistryFile file)> registryVersionsByHash)
        {
            foreach (var script in saves.Scripts)
            {
                if (!registryVersionsByHash.TryGetValue(script.Hash, out var svf))
                {
                    continue;
                }

                if (script is ScriptList scriptList)
                {
                    if (!scriptList.Scripts.All(s => svf.version.Files.Any(f => s.Hash == f.Hash.Value)))
                        continue;
                }

                yield return new RegistrySavesMatch
                {
                    Script = svf.script,
                    Version = svf.version,
                    File = svf.file,
                    Local = script
                };
            }
        }
    }
}
