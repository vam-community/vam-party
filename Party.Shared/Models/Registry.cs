using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Party.Shared.Exceptions;

namespace Party.Shared.Models
{
    public class Registry
    {
        public List<RegistryScript> Scripts { get; set; }
    }

    public class RegistryScript
    {
        public static readonly Regex ValidNameRegex = new Regex(@"^[a-z][a-z0-9\-_]{2,127}$");

        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public RegistryScriptAuthor Author { get; set; }
        // TODO: Ensure this only contains valid characters
        public string Homepage { get; set; }
        public string Repository { get; set; }
        public List<RegistryScriptVersion> Versions { get; set; }

        public RegistryScriptVersion GetLatestVersion()
        {
            // TODO: String sorting will not cut it
            return SortedVersions().FirstOrDefault();
        }

        public IEnumerable<RegistryScriptVersion> SortedVersions()
        {
            return Versions.OrderByDescending(v => v.Version);
        }
    }

    public class RegistryScriptAuthor
    {
        public string Name { get; set; }
        public string Profile { get; set; }
    }

    public class RegistryScriptVersion
    {
        public static readonly Regex ValidVersionNameRegex = new Regex(@"^(?<Major>0|[1-9][0-9]{0,3})\.(?<Minor>0|[1-9][0-9]{0,3})\.(?<Revision>0|[1-9][0-9]{0,3})(-(?<Extra>[a-z0-9]{1,32}))?$", RegexOptions.Compiled);

        public RegistryVersionString Version { get; set; }
        public List<RegistryFile> Files { get; set; }
    }

    public class RegistryFile
    {
        // TODO: Ensure this only contains valid characters
        public string Filename { get; set; }
        public string Url { get; set; }
        public RegistryFileHash Hash { get; set; }
    }

    public class RegistryFileHash
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public struct RegistryVersionString : IComparable<RegistryVersionString>
    {
        public RegistryVersionString(string version)
        {
            var result = RegistryScriptVersion.ValidVersionNameRegex.Match(version);
            if (!result.Success) throw new RegistryException($"Invalid version number: '{version}'");
            Major = int.Parse(result.Groups["Major"].Value);
            Minor = int.Parse(result.Groups["Minor"].Value);
            Revision = int.Parse(result.Groups["Revision"].Value);
            Extra = result.Groups["Extra"].Value;
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Revision { get; set; }
        public string Extra { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RegistryVersionString rvs && Major == rvs.Major && Minor == rvs.Minor && Revision == rvs.Revision && Extra == rvs.Extra;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Revision, Extra);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Extra))
                return $"{Major}.{Minor}.{Revision}";
            else
                return $"{Major}.{Minor}.{Revision}-{Extra}";
        }

        int IComparable<RegistryVersionString>.CompareTo(RegistryVersionString other)
        {
            if (Major != other.Major)
                if (Major > other.Major)
                    return 1;
                else
                    return -1;

            if (Minor != other.Minor)
                if (Minor > other.Minor)
                    return 1;
                else
                    return -1;

            if (Revision != other.Revision)
                if (Revision > other.Revision)
                    return 1;
                else
                    return -1;

            return Extra.CompareTo(other.Extra);
        }

        public static implicit operator string(RegistryVersionString d) => d.ToString();
        public static implicit operator RegistryVersionString(string b) => new RegistryVersionString(b);
    }
}
