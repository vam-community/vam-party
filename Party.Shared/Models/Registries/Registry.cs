using System;
using System.Collections.Generic;
using System.Linq;

namespace Party.Shared.Models.Registries
{
    public class Registry
    {
        private Dictionary<RegistryPackageType, IReadOnlyCollection<RegistryPackage>> _groupedByType;

        public SortedSet<RegistryAuthor> Authors { get; set; } = new SortedSet<RegistryAuthor>();
        public SortedSet<RegistryPackage> Packages { get; set; } = new SortedSet<RegistryPackage>();

        public RegistryPackage GetPackage(PackageFullName packageName)
        {
            return Get(packageName.Type).FirstOrDefault(s => s.Name.Equals(packageName.Name, StringComparison.InvariantCultureIgnoreCase));
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

            script = new RegistryPackage { Name = name, Versions = new SortedSet<RegistryPackageVersion>() };
            Packages.Add(script);
            _groupedByType = null;
            return script;
        }
    }
}
