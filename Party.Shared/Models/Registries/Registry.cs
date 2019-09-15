using System;
using System.Collections.Generic;
using System.Linq;

namespace Party.Shared.Models.Registries
{
    public class Registry
    {
        public SortedSet<RegistryAuthor> Authors { get; set; } = new SortedSet<RegistryAuthor>();
        public RegistryPackageGroups Packages { get; set; } = new RegistryPackageGroups();

        public RegistryPackage GetPackage(PackageFullName packageName)
        {
            return Packages.Get(packageName.Type).FirstOrDefault(s => s.Name.Equals(packageName.Name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
