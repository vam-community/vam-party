namespace Party.Shared.Resources
{
    public class ScriptList : Script
    {
        private Script[] Scripts { get; }

        public ScriptList(string fullPath, string hash, Script[] scripts)
        : base(fullPath, hash)
        {
            Scripts = scripts;
        }
    }
}
