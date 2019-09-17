using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Party.Shared.Models.Registries
{
    public class RegistryPackage : IComparable<RegistryPackage>, IComparable
    {
        public static readonly Regex ValidNameRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9\-_ ]{2,127}$");

        public RegistryPackageType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public string Author { get; set; }
        // TODO: Ensure this only contains valid characters
        public string Homepage { get; set; }
        public string Repository { get; set; }
        public SortedSet<RegistryPackageVersion> Versions { get; set; }

        public RegistryPackageVersion GetLatestVersion()
        {
            return Versions?.FirstOrDefault();
        }

        public RegistryPackageVersion GetVersion(string version)
        {
            return Versions?.FirstOrDefault(p => p.Version.ToString().Equals(version));
        }

        public RegistryPackageVersion CreateVersion()
        {
            var version = new RegistryPackageVersion();
            if (!Versions.Add(version)) throw new InvalidOperationException("Could not create a new version");
            return version;
        }

        int IComparable<RegistryPackage>.CompareTo(RegistryPackage other)
        {
            return Name?.CompareTo(other.Name) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryPackage>).CompareTo(obj as RegistryPackage);
        }
    }
}
