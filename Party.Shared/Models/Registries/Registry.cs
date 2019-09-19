using System;
using System.Collections.Generic;
using System.Linq;
using Party.Shared.Exceptions;

namespace Party.Shared.Models.Registries
{
    public class Registry
    {
        private Dictionary<RegistryPackageType, IReadOnlyCollection<RegistryPackage>> _groupedByType;

        public SortedSet<RegistryAuthor> Authors { get; set; } = new SortedSet<RegistryAuthor>();
        public SortedSet<RegistryPackage> Packages { get; set; } = new SortedSet<RegistryPackage>();

        public RegistryPackage GetPackage(PackageFullName packageName)
        {
            return GetPackage(packageName.Type, packageName.Name);
        }

        public RegistryPackage GetPackage(RegistryPackageType type, string name)
        {
            return Get(type).FirstOrDefault(s => s.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public IReadOnlyCollection<RegistryPackage> Get(RegistryPackageType type)
        {
            if (_groupedByType == null)
            {
                _groupedByType = Packages
                .GroupBy(package => package.Type)
                .ToDictionary(
                    group => group.Key,
                    group => new SortedSet<RegistryPackage>(group) as IReadOnlyCollection<RegistryPackage>);
            }

            return _groupedByType.TryGetValue(type, out var set) ? set : new SortedSet<RegistryPackage>();
        }

        public RegistryPackage GetOrCreatePackage(RegistryPackageType type, string name)
        {
            // TODO: Script-specific
            var script = Get(type).FirstOrDefault(s => s.Name == name);
            if (script != null) return script;

            script = new RegistryPackage { Type = RegistryPackageType.Scripts, Name = name, Versions = new SortedSet<RegistryPackageVersion>() };
            Packages.Add(script);
            _groupedByType = null;
            return script;
        }

        public bool TryGetDependency(RegistryPackageDependency dependency, out RegistryPackageVersionContext context)
        {
            var package = Get(dependency.Type)?.FirstOrDefault(p => p.Name.Equals(dependency.Name, StringComparison.InvariantCultureIgnoreCase));
            if (package == null)
            {
                context = null;
                return false;
            }
            var version = dependency.Version != null ? package.GetLatestVersion() : package.Versions.FirstOrDefault(v => v.Version.Equals(dependency.Version));
            if (version == null)
            {
                context = null;
                return false;
            }

            context = new RegistryPackageVersionContext(this, package, version);
            return true;
        }

        public IEnumerable<RegistryPackageFileContext> FlattenFiles(RegistryPackageType type)
        {
            // TODO: Script-specific
            return Get(type)
                .SelectMany(package => package.Versions.Select(version => new RegistryPackageVersionContext(this, package, version))
                .SelectMany(context => context.Version.Files.Select(file => new RegistryPackageFileContext(context, file))));
        }

        public void AssertNoDuplicates(RegistryPackageType type, RegistryPackageVersion version)
        {
            // TODO: Script-specific
            var hashes = version.Files.Where(f => f.Hash?.Value != null).Select(f => f.Hash.Value).ToArray();
            var conflictingVersion = Get(type)
                .SelectMany(s => s.Versions.Where(v => !ReferenceEquals(v, version)).Select(v => (s, v)))
                .FirstOrDefault(x => x.v.Files.Count == hashes.Length && x.v.Files.All(f => hashes.Contains(f.Hash?.Value)));

            if (conflictingVersion.v != null)
                throw new UserInputException($"This version contains exactly the same file count and file hashes as {conflictingVersion.s.Name} v{conflictingVersion.v.Version}.");
        }
    }
}
