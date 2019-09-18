using System;
using System.Linq;

namespace Party.Shared.Models.Registries
{
    public class RegistryFile : IComparable<RegistryFile>, IComparable
    {
        // TODO: Ensure this only contains valid characters
        public string Filename { get; set; }
        public string Url { get; set; }
        public RegistryHash Hash { get; set; }
        public bool Ignore { get; set; }

        int IComparable<RegistryFile>.CompareTo(RegistryFile other)
        {
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
