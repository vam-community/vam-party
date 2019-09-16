using System.Collections.Generic;

namespace Party.Shared.Models.Local
{
    public class LocalSceneFile : LocalFile
    {
        public List<LocalScriptFile> Scripts { get; } = new List<LocalScriptFile>();

        public LocalSceneFile(string fullPath)
            : base(fullPath, null)
        {
        }

        internal void References(LocalScriptFile script)
        {
            Scripts.Add(script);
        }
    }
}
