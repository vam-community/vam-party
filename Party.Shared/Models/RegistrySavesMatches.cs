namespace Party.Shared.Models
{
    public class RegistrySavesMatches
    {
        public RegistrySavesMatch[] HashMatches { get; set; }
        public RegistrySavesMatch[] FilenameMatches { get; set; }
        public Script[] NoMatches { get; set; }
    }

    public class RegistrySavesMatch
    {
        public RegistryScript Script { get; set; }
        public RegistryScriptVersion Version { get; set; }
        public RegistryFile File { get; set; }
        public Script Local { get; set; }

        public RegistrySavesMatch()
        {
        }

        public RegistrySavesMatch((RegistryScript script, RegistryScriptVersion version, RegistryFile file) svf, Script local)
        {
            Script = svf.script;
            Version = svf.version;
            File = svf.file;
            Local = local;
        }
    }
}
