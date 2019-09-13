using System;

namespace Party.Shared.Models.Registries
{
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
