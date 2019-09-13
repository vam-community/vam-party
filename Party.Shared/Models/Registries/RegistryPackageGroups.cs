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
            switch (type)
            {
                case PackageTypes.Scripts:
                    return Scripts;
                case PackageTypes.Clothing:
                    return Clothing;
                case PackageTypes.Scenes:
                    return Scenes;
                case PackageTypes.Textures:
                    return Textures;
                case PackageTypes.Assets:
                    return Assets;
                case PackageTypes.Morphs:
                    return Morphs;
                default:
                    throw new NotImplementedException($"Unknown package type: {type}");
            }
        }
    }
}
