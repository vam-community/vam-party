using System;
using System.Text.RegularExpressions;

namespace Party.Shared.Models.Registries
{
    public class PackageFullName
    {
        private static readonly Regex _regex = new Regex(@"^(?<type>[a-z]+)/(?<name>[a-z0-9_\-]+)(@(?<version>[0-9\.\-]+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParsePackage(string name, out PackageFullName info)
        {
            if (name == null)
            {
                info = null;
                return false;
            }

            var match = _regex.Match(name);

            if (!match.Success)
            {
                info = null;
                return false;
            }

            if (!Enum.TryParse<RegistryPackageType>(match.Groups["type"].Value, true, out var type) || type == RegistryPackageType.Unknown)
            {
                info = null;
                return false;
            }

            info = new PackageFullName
            {
                Type = type,
                Name = match.Groups["name"].Value,
                Version = match.Groups["version"].Success ? match.Groups["version"].Value : null
            };
            return true;
        }

        public string Name { get; set; }
        public RegistryPackageType Type { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            return $"{Type}/{Name}";
        }
    }
}
