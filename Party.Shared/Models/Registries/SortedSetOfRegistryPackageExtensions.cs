using System.Collections.Generic;
using System.Linq;
using Party.Shared.Exceptions;

namespace Party.Shared.Models.Registries
{
    public static class SortedSetOfRegistryPackageExtensions
    {
        public static RegistryPackage GetOrCreatePackage(this SortedSet<RegistryPackage> set, string name)
        {
            // TODO: Script-specific
            var script = set.FirstOrDefault(s => s.Name == name);
            if (script != null) return script;

            script = new RegistryPackage { Name = name, Versions = new SortedSet<RegistryPackageVersion>() };
            set.Add(script);
            return script;
        }

        public static IEnumerable<(RegistryPackage package, RegistryPackageVersion version, RegistryFile file)> FlattenFiles(this SortedSet<RegistryPackage> set)
        {
            // TODO: Script-specific
            return set
                .SelectMany(script => script.Versions.Select(version => (script, version))
                .SelectMany(sv => sv.version.Files.Select(file => (sv.script, sv.version, file))));
        }

        public static void AssertNoDuplicates(this SortedSet<RegistryPackage> set, RegistryPackageVersion version)
        {
            // TODO: Script-specific
            var hashes = version.Files.Where(f => f.Hash?.Value != null).Select(f => f.Hash.Value).ToArray();
            var conflictingVersion = set
                .SelectMany(s => s.Versions.Where(v => !ReferenceEquals(v, version)).Select(v => (s, v)))
                .FirstOrDefault(x => x.v.Files.Count == hashes.Length && x.v.Files.All(f => hashes.Contains(f.Hash?.Value)));

            if (conflictingVersion.v != null)
                throw new UserInputException($"This version contains exactly the same file count and file hashes as {conflictingVersion.s.Name} v{conflictingVersion.v.Version}.");

            // TODO: Also assert there are no conflicting LocalPath in another package, otherwise either make a dependency or ask for a resolution in GitHub
        }
    }
}
