namespace Party.Shared.Models
{
    public class InstalledPackageInfoResult
    {
        public InstalledFileInfo[] Files { get; set; }
        public string InstallFolder { get; set; }

        public enum FileStatus
        {
            NotInstalled,
            Installed,
            HashMismatch
        }

        public class InstalledFileInfo
        {
            public string Path { get; set; }
            public FileStatus Status { get; set; }
            public RegistryFile RegistryFile { get; set; }
        }
    }
}
