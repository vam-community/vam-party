using System.Collections.Generic;

namespace Party.Shared.Models.Local
{
    public class LocalScriptFile : LocalFile
    {
        public HashSet<LocalSceneFile> Scenes { get; } = new HashSet<LocalSceneFile>();

        public LocalScriptFile(string fullPath, string hash)
        : base(fullPath, hash)
        {
        }

        internal void ReferencedBy(LocalSceneFile scene)
        {
            Scenes.Add(scene);
        }
    }
}
