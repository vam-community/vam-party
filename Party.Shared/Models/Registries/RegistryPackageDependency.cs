using System;
using System.Collections.Generic;

namespace Party.Shared.Models.Registries
{
    public class RegistryPackageDependency : IComparable<RegistryPackageDependency>, IComparable
    {
        public RegistryPackageType Type { get; set; }
        public string Name { get; set; }
        public RegistryVersionString Version { get; set; }
        public List<string> Files { get; set; }

        public override string ToString()
        {
            return $"{Type.ToString().ToLowerInvariant()}/{Name}@{Version}";
        }

        int IComparable<RegistryPackageDependency>.CompareTo(RegistryPackageDependency other)
        {
            return Name?.CompareTo(other.Name) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryPackageDependency>).CompareTo(obj as RegistryPackageDependency);
        }
    }
}
