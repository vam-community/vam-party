using System;

namespace Party.Shared.Models.Registries
{
    public class RegistryPackageDependency : IComparable<RegistryPackageDependency>, IComparable
    {
        public string Name { get; set; }
        public string Version { get; set; }

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
