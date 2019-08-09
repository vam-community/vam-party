using Party.Shared.Commands;

namespace Party.CLI
{
    public class DefaultConfiguration
    {
        public static PartyConfiguration Get()
        {
            return new PartyConfiguration
            {
                VirtAMate = new PartyConfigurationVirtAMate
                {
                    SavesDirectory = "Saves"
                },
                Scanning = new PartyConfigurationScanning
                {
                    Ignore = new[] { "scene\\MeshedVR", "Person", "Downloads" }
                },
                Registry = new PartyConfigurationRegistry
                {
                    TrustedDomains = new[]
                    {
                        "https://github.com/",
                        "https://raw.githubusercontent.com/"
                    },
                    Urls = new[]
                    {
                        "https://raw.githubusercontent.com/vam-community/vam-registry/master/v1/index.json"
                    }
                }
            };
        }
    }
}
