using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Party.Shared.Exceptions;

namespace Party.Shared.Models
{
    public class Registry
    {
        private SortedSet<RegistryScript> _scripts;
        private SortedSet<RegistryAuthor> _authors;

        public SortedSet<RegistryAuthor> Authors { get => _authors ?? (_authors = new SortedSet<RegistryAuthor>()); set => _authors = value; }
        public SortedSet<RegistryScript> Scripts { get => _scripts ?? (_scripts = new SortedSet<RegistryScript>()); set => _scripts = value; }

        public RegistryScript GetOrCreateScript(string name)
        {
            var script = Scripts.FirstOrDefault(s => s.Name == name);
            if (script != null) return script;

            script = new RegistryScript { Name = name, Versions = new SortedSet<RegistryScriptVersion>() };
            Scripts.Add(script);
            return script;
        }

        public void AssertNoDuplicates(RegistryScriptVersion version)
        {
            var hashes = version.Files.Where(f => f.Hash?.Value != null).Select(f => f.Hash.Value).ToArray();
            var match = Scripts
                .SelectMany(s => s.Versions.Where(v => !ReferenceEquals(v, version)).Select(v => (s, v)))
                .FirstOrDefault(x => x.v.Files.Count == hashes.Length && x.v.Files.All(f => hashes.Contains(f.Hash?.Value)));

            if (match.v != null)
                throw new UserInputException($"This version contains exactly the same file count and file hashes as {match.s.Name} v{match.v.Version}.");
        }

        public IEnumerable<(RegistryScript script, RegistryScriptVersion version, RegistryFile file)> FlattenFiles()
        {
            return Scripts
                .SelectMany(script => script.Versions.Select(version => (script, version))
                .SelectMany(sv => sv.version.Files.Select(file => (sv.script, sv.version, file))));
        }
    }

    public class RegistryScript : IComparable<RegistryScript>, IComparable
    {
        public static readonly Regex ValidNameRegex = new Regex(@"^[a-z][a-z0-9\-_]{2,127}$");

        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public string Author { get; set; }
        // TODO: Ensure this only contains valid characters
        public string Homepage { get; set; }
        public string Repository { get; set; }
        public SortedSet<RegistryScriptVersion> Versions { get; set; }

        public RegistryScriptVersion GetLatestVersion()
        {
            return Versions.FirstOrDefault();
        }

        public RegistryScriptVersion CreateVersion()
        {
            var version = new RegistryScriptVersion();
            if (!Versions.Add(version)) throw new InvalidOperationException("Could not create a new version");
            return version;
        }

        int IComparable<RegistryScript>.CompareTo(RegistryScript other)
        {
            return Name?.CompareTo(other.Name) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryScript>).CompareTo(obj as RegistryScript);
        }
    }

    public class RegistryScriptVersion : IComparable<RegistryScriptVersion>, IComparable
    {
        public static readonly Regex ValidVersionNameRegex = new Regex(@"^(?<Major>0|[1-9][0-9]{0,3})\.(?<Minor>0|[1-9][0-9]{0,3})\.(?<Revision>0|[1-9][0-9]{0,3})(-(?<Extra>[a-z0-9]{1,32}))?$", RegexOptions.Compiled);

        private SortedSet<RegistryFile> _files;

        public RegistryVersionString Version { get; set; }
        public DateTimeOffset Created { get; set; }
        public string Notes { get; set; }
        public SortedSet<RegistryFile> Files { get => _files ?? (_files = new SortedSet<RegistryFile>()); set => _files = value; }

        int IComparable<RegistryScriptVersion>.CompareTo(RegistryScriptVersion other)
        {
            return (Version as IComparable<RegistryVersionString>).CompareTo(other.Version);
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryScriptVersion>).CompareTo(obj as RegistryScriptVersion);
        }
    }

    public class RegistryFile : IComparable<RegistryFile>, IComparable
    {
        // TODO: Ensure this only contains valid characters
        public string Filename { get; set; }
        public string LocalPath { get; set; }
        public string Url { get; set; }
        public RegistryFileHash Hash { get; set; }
        public bool Ignore { get; set; }

        int IComparable<RegistryFile>.CompareTo(RegistryFile other)
        {
            var thisSlashes = Filename?.Count(c => c == '/') ?? 0;
            var otherSlashes = other.Filename?.Count(c => c == '/') ?? 0;
            if (thisSlashes > otherSlashes)
                return 1;
            else if (thisSlashes < otherSlashes)
                return -1;
            return (Filename ?? LocalPath)?.CompareTo(other.Filename ?? other.LocalPath) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryFile>).CompareTo(obj as RegistryFile);
        }
    }

    public class RegistryFileHash
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public struct RegistryVersionString : IComparable, IComparable<RegistryVersionString>, IEquatable<RegistryVersionString>
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

    public class RegistryAuthor : IComparable<RegistryAuthor>, IComparable
    {
        public string Name { get; set; }
        public string Reddit { get; set; }
        public string Github { get; set; }

        int IComparable<RegistryAuthor>.CompareTo(RegistryAuthor other)
        {
            return Name?.CompareTo(other.Name) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryAuthor>).CompareTo(obj as RegistryAuthor);
        }
    }
}
