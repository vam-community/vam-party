using System;
using System.Collections.Generic;

namespace Party.Shared.Models.Registries
{
    public class RegistryPackageGroups
    {
        public SortedSet<RegistryPackage> Scripts { get; set; } = new SortedSet<RegistryPackage>();
        public SortedSet<RegistryPackage> Scenes { get; set; } = new SortedSet<RegistryPackage>();
        public SortedSet<RegistryPackage> Morphs { get; set; } = new SortedSet<RegistryPackage>();
        public SortedSet<RegistryPackage> Clothing { get; set; } = new SortedSet<RegistryPackage>();
        public SortedSet<RegistryPackage> Assets { get; set; } = new SortedSet<RegistryPackage>();
        public SortedSet<RegistryPackage> Textures { get; set; } = new SortedSet<RegistryPackage>();

        public SortedSet<RegistryPackage> Get(PackageTypes type)
        {
            return type switch
            {
                PackageTypes.Scripts => Scripts,
                PackageTypes.Clothing => Clothing,
                PackageTypes.Scenes => Scenes,
                PackageTypes.Textures => Textures,
                PackageTypes.Assets => Assets,
                PackageTypes.Morphs => Morphs,
                _ => throw new NotImplementedException($"Unknown package type: {type}"),
            };
        }
    }
}
