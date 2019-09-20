using System.Collections.Generic;

namespace Party.Shared.Models.Local
{
    public class LocalSceneFile : LocalFile
    {
        private readonly object _sync = new object();

        public HashSet<LocalScriptFile> Scripts { get; } = new HashSet<LocalScriptFile>();

        public LocalSceneFile(string fullPath)
            : base(fullPath, null)
        {
        }

        internal void References(LocalScriptFile script)
        {
            lock (_sync)
                Scripts.Add(script);
        }
    }
}
