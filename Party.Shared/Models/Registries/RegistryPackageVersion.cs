using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Party.Shared.Models.Registries
{
    public class RegistryPackageVersion : IComparable<RegistryPackageVersion>, IComparable
    {
        public static readonly Regex ValidVersionNameRegex = new Regex(@"^(?<Major>0|[1-9][0-9]{0,3})\.(?<Minor>0|[1-9][0-9]{0,3})\.(?<Revision>0|[1-9][0-9]{0,3})(-(?<Extra>[a-z0-9]{1,32}))?$", RegexOptions.Compiled);

        private SortedSet<RegistryFile> _files;

        public RegistryVersionString Version { get; set; }
        public DateTimeOffset Created { get; set; }
        public string DownloadUrl { get; set; }
        public string Notes { get; set; }
        public SortedSet<RegistryPackageDependency> Dependencies { get; set; }
        public SortedSet<RegistryFile> Files { get => _files ?? (_files = new SortedSet<RegistryFile>()); set => _files = value; }

        int IComparable<RegistryPackageVersion>.CompareTo(RegistryPackageVersion other)
        {
            return (Version as IComparable<RegistryVersionString>).CompareTo(other.Version);
        }

        int IComparable.CompareTo(object obj)
        {
            return (this as IComparable<RegistryPackageVersion>).CompareTo(obj as RegistryPackageVersion);
        }
    }
}
