namespace Party.Shared.Commands
{
    public class PartyConfiguration
    {
        public PartyConfigurationVirtAMate VirtAMate { get; set; }
        public PartyConfigurationScanning Scanning { get; set; }
        public PartyConfigurationRegistry Registry { get; set; }
    }

    public class PartyConfigurationVirtAMate
    {
        public string SavesDirectory { get; set; }
    }

    public class PartyConfigurationScanning
    {
        public string[] Ignore { get; set; }
    }

    public class PartyConfigurationRegistry
    {
        public string[] TrustedDomains { get; set; }
        public string[] Urls { get; set; }
    }
}
