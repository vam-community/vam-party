namespace Party.Shared.Results
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
            public RegistryResult.RegistryFile RegistryFile { get; set; }
        }
    }
}
