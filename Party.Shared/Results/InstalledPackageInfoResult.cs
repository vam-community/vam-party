namespace Party.Shared.Results
{
    public class InstalledPackageInfoResult
    {
        public InstalledFileInfo[] Files { get; internal set; }
        public string InstallFolder { get; set; }

        public enum FileStatus
        {
            NotInstalled,
            Installed,
            HashMismatch
        }

        public class InstalledFileInfo
        {
            public string Path { get; internal set; }
            public FileStatus Status { get; internal set; }
            public RegistryResult.RegistryFile RegistryFile { get; internal set; }
        }
    }
}
