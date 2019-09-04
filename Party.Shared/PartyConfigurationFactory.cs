using System.IO;

namespace Party.Shared
{
    public static class PartyConfigurationFactory
    {
        public static PartyConfiguration Create(string baseDirectory)
        {
            return new PartyConfiguration
            {
                VirtAMate = new PartyConfigurationVirtAMate(baseDirectory)
                {
                    VirtAMateInstallFolder = "./",
                    VirtAMateAllowedSubfolders = new[] { "Assets", "Import", "Saves", "Textures" }
                },
                Scanning = new PartyConfigurationScanning
                {
                    Ignore = new[] { Path.Combine("scene", "MeshedVR"), "Person", "Downloads", "Dev" },
                    PackagesFolder = "party"
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
