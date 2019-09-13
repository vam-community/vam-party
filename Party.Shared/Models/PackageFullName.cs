using System;
using Party.Shared.Models.Registries;

namespace Party.Shared.Models
{
    public class PackageFullName
    {
        public static bool TryParsePackage(string name, out PackageFullName info)
        {
            info = null;
            var parts = name.Split('/');
            if (parts.Length != 2)
                return false;

            if (!Enum.TryParse<PackageTypes>(parts[0], true, out var type))
                return false;

            if (!RegistryPackage.ValidNameRegex.IsMatch(parts[1]))
                return false;

            info = new PackageFullName
            {
                Type = type,
                Name = parts[1]
            };
            return true;
        }

        public string Name { get; set; }
        public PackageTypes Type { get; set; }

        public override string ToString()
        {
            return $"{Type}/{Name}";
        }
    }
}
