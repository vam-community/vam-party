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
            var thisSlashes = Filename?.Count(c => c == '/') ?? 0;
            var otherSlashes = other.Filename?.Count(c => c == '/') ?? 0;
            if (thisSlashes > otherSlashes)
                return 1;
            else if (thisSlashes < otherSlashes)
                return -1;
            return Filename?.CompareTo(other.Filename) ?? 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryFile>).CompareTo(obj as RegistryFile);
        }
    }
}
