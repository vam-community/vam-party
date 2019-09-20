using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Party.Shared.Models.Registries
{
    public class RegistryFile : IComparable<RegistryFile>, IComparable
    {
        public static readonly Regex ValidFilename = new Regex(@"^\/?([^\/:""*?|<>\.]+\/)*([^\/:""*?|<>\.]+\.)+[a-z]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public string Filename { get; set; }
        public string Url { get; set; }
        public RegistryHash Hash { get; set; }
        public bool Ignore { get; set; }

        public override string ToString()
        {
            if (Filename == null)
                return "{(no filename)}";
            if (Ignore)
                return $"{{'{Filename}' -> [ignored]}}";
            if (Hash?.Value != null)
                return $"{{'{Filename}' -> {Hash?.Value}}}";
            return $"{{'{Filename}'}}";
        }

        int IComparable<RegistryFile>.CompareTo(RegistryFile other)
        {
            if (Filename == null && other.Filename == null)
                return 0;

            // Anything without a filename is sent down
            if (Filename != null && other.Filename == null)
                return -1;
            else if (Filename == null && other.Filename != null)
                return 1;

            // Anything with a URL first
            if (Url != null && other.Url == null)
                return -1;
            else if (Url == null && other.Url != null)
                return 1;

            // Then, anything with at least a hash
            if (Hash?.Value != null && other.Hash?.Value == null)
                return -1;
            else if (Hash?.Value == null && other.Hash?.Value != null)
                return 1;

            if (Filename.StartsWith("/") && !other.Filename.StartsWith("/"))
                return 1;
            else if (!Filename.StartsWith("/") && other.Filename.StartsWith("/"))
                return -1;

            // Start with folders at the root level
            var thisSlashes = Filename?.Count(c => c == '/') ?? 0;
            var otherSlashes = other.Filename?.Count(c => c == '/') ?? 0;
            if (thisSlashes > otherSlashes)
                return 1;
            else if (thisSlashes < otherSlashes)
                return -1;

            // Finally sort by filename
            return Filename?.CompareTo(other.Filename) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryFile>).CompareTo(obj as RegistryFile);
        }
    }
}
