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
        public RegistryPackage Package { get; set; }
        public RegistryPackageVersion Version { get; set; }
        public RegistryFile File { get; set; }
        public LocalScriptFile Local { get; set; }

        public RegistrySavesMatch()
        {
        }

        public RegistrySavesMatch((RegistryPackage script, RegistryPackageVersion version, RegistryFile file) svf, LocalScriptFile local)
        {
            Package = svf.script;
            Version = svf.version;
            File = svf.file;
            Local = local;
        }
    }
}
