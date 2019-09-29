using System;
using Party.Shared.Exceptions;

namespace Party.Shared.Models.Registries
{
    public struct RegistryVersionString : IComparable, IComparable<RegistryVersionString>, IEquatable<RegistryVersionString>
    {
        public static bool IsEmpty(RegistryVersionString value)
        {
            return value.Extra == null;
        }

        public static RegistryVersionString Parse(string version)
        {
            var result = RegistryPackageVersion.ValidVersionNameRegex.Match(version);
            if (!result.Success) throw new RegistryException($"Invalid version number: '{version}'");
            return new RegistryVersionString(
                int.Parse(result.Groups["Major"].Value),
                int.Parse(result.Groups["Minor"].Value),
                int.Parse(result.Groups["Revision"].Value),
                result.Groups["Extra"].Value);
        }

        public RegistryVersionString(int major, int minor, int revision, string extra)
        {
            Major = major;
            Minor = minor;
            Revision = revision;
            Extra = extra ?? throw new ArgumentNullException(nameof(extra));
        }

        public int Major { get; }
        public int Minor { get; }
        public int Revision { get; }
        public string Extra { get; }

        public override bool Equals(object obj)
        {
            if (obj is RegistryVersionString vStr)
                return (this as IEquatable<RegistryVersionString>).Equals(vStr);
            else if (obj is string str)
                return (this as IEquatable<RegistryVersionString>).Equals(Parse(str));
            else
                return false;
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
        public static implicit operator RegistryVersionString(string b) => Parse(b);
    }
}
