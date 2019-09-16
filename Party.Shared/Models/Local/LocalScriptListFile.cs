namespace Party.Shared.Models.Local
{
    public class LocalScriptListFile : LocalScriptFile
    {
        public LocalScriptFile[] Scripts { get; }

        public LocalScriptListFile(string fullPath, string hash, LocalScriptFile[] scripts)
        : base(fullPath, hash)
        {
            Scripts = scripts;
        }
    }
}
