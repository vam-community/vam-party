using System.IO;

namespace Party.Shared
{
    public class PartyConfiguration
    {
        public PartyConfigurationVirtAMate VirtAMate { get; set; }
        public PartyConfigurationRegistry Registry { get; set; }
    }

    public class PartyConfigurationVirtAMate
    {
        private readonly string _baseDirectory;
        private string _virtAMateInstallFolder;

        public PartyConfigurationVirtAMate(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public string VirtAMateInstallFolder
        {
            get => _virtAMateInstallFolder;
            set => _virtAMateInstallFolder = Path.GetFullPath(value, _baseDirectory);
        }

        public string[] AllowedSubfolders { get; set; }
        public string[] IgnoredFolders { get; set; }
        public string PackagesFolder { get; set; }
    }

    public class PartyConfigurationRegistry
    {
        public string[] TrustedDomains { get; set; }
        public string[] Urls { get; set; }
    }
}
