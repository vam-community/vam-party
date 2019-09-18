using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

namespace Party.Shared.Models
{
    public class RegistrySavesMatches
    {
        public RegistrySavesMatch[] HashMatches { get; set; }
        public RegistrySavesMatch[] FilenameMatches { get; set; }
        public LocalScriptFile[] NoMatches { get; set; }
    }

    public class RegistrySavesMatch
    {
        public RegistryPackageFileContext Remote { get; set; }
        public LocalScriptFile Local { get; set; }

        public RegistrySavesMatch()
        {
        }

        public RegistrySavesMatch(RegistryPackageFileContext remote, LocalScriptFile local)
        {
            Remote = remote;
            Local = local;
        }
    }
}
