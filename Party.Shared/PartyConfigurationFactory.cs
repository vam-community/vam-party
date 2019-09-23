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
                    AllowedSubfolders = new[] { "Custom", "Saves" },
                    IgnoredFolders = new[]
                    {
                        Path.Combine("Saves", "scene", "MeshedVR"),
                        Path.Combine("Saves", "Person"),
                        Path.Combine("Saves", "Downloads"),
                        Path.Combine("Custom", "Scripts", "Dev"),
                        Path.Combine("Custom", "Scripts", "MeshedVR"),
                    }
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
