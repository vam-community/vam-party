using System.IO;

namespace Party.Shared
{
    public class PartyConfiguration
    {
        public PartyConfigurationVirtAMate VirtAMate { get; set; }
        public PartyConfigurationScanning Scanning { get; set; }
        public PartyConfigurationRegistry Registry { get; set; }
    }

    public class PartyConfigurationVirtAMate
    {
        private readonly string _baseDirectory;
        private string _savesDirectory;

        public PartyConfigurationVirtAMate(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }
        public string SavesDirectory
        {
            get => _savesDirectory;
            set => _savesDirectory = Path.GetFullPath(value, _baseDirectory);
        }
    }

    public class PartyConfigurationScanning
    {
        public string[] Ignore { get; set; }
        public string PackagesFolder { get; set; }
    }

    public class PartyConfigurationRegistry
    {
        public string[] TrustedDomains { get; set; }
        public string[] Urls { get; set; }
    }
}
