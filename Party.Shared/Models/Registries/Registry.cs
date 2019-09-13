using System.Collections.Generic;

namespace Party.Shared.Models.Registries
{
    public class Registry
    {
        public SortedSet<RegistryAuthor> Authors { get; set; } = new SortedSet<RegistryAuthor>();
        public RegistryPackageGroups Packages { get; set; } = new RegistryPackageGroups();
    }
}
