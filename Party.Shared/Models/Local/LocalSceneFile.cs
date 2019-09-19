using System.Collections.Generic;

namespace Party.Shared.Models.Local
{
    public class LocalSceneFile : LocalFile
    {
        public HashSet<LocalScriptFile> Scripts { get; } = new HashSet<LocalScriptFile>();

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
