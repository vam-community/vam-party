using System;
using Party.Shared.Exceptions;

namespace Party.Shared.Models.Registries
{
    public struct RegistryVersionString : IComparable, IComparable<RegistryVersionString>, IEquatable<RegistryVersionString>
    {
        public RegistryVersionString(string version)
        {
            var result = RegistryPackageVersion.ValidVersionNameRegex.Match(version);
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
#if NETSTANDARD2_1
            return HashCode.Combine(Major, Minor, Revision, Extra);
#else
            return (Major * 100000000) + (Minor * 100000) + (Revision * 100) + (Extra?.GetHashCode() ?? 0);
#endif
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
            {
                if (Major > other.Major)
                    return -1;
                else
                    return 1;
            }

            if (Minor != other.Minor)
            {
                if (Minor > other.Minor)
                    return -1;
                else
                    return 1;
            }

            if (Revision != other.Revision)
            {
                if (Revision > other.Revision)
                    return -1;
                else
                    return 1;
            }

            return string.CompareOrdinal(other.Extra, Extra);
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryVersionString>).CompareTo((RegistryVersionString)obj);
        }

        bool IEquatable<RegistryVersionString>.Equals(RegistryVersionString other)
        {
            return Major == other.Major && Minor == other.Minor && Revision == other.Revision && Extra == other.Extra;
        }

        public static implicit operator string(RegistryVersionString d) => d.ToString();
        public static implicit operator RegistryVersionString(string b) => new RegistryVersionString(b);
    }
}
