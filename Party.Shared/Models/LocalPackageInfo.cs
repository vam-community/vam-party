using Party.Shared.Models.Registries;

namespace Party.Shared.Models
{
    public class LocalPackageInfo
    {
        public InstalledFileInfo[] Files { get; set; }
        public string PackageFolder { get; set; }
        public bool Installed { get; set; }
        public bool Installable { get; set; }
        public bool Corrupted { get; set; }
    }

    public enum FileStatus
    {
        NotInstalled,
        Installed,
        HashMismatch,
        Ignored,
        NotDownloadable
    }

    public class InstalledFileInfo
    {
        public string FullPath { get; set; }
        public FileStatus Status { get; set; }
        public RegistryFile RegistryFile { get; set; }
        public string Reason { get; set; }
    }
}
