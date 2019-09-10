using System.Linq;

namespace Party.Shared.Models
{
    public class LocalPackageInfo
    {
        public InstalledFileInfo[] Files { get; set; }
        public string InstallFolder { get; set; }

        public FileStatus[] DistinctStatuses() => Files.Select(f => f.Status).Distinct().ToArray();

        public enum FileStatus
        {
            NotInstalled,
            Installed,
            HashMismatch,
            Ignored
        }

        public class InstalledFileInfo
        {
            public string Path { get; set; }
            public FileStatus Status { get; set; }
            public RegistryFile RegistryFile { get; set; }
        }
    }
}
