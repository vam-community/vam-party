using System.Collections.Generic;

namespace Party.Shared.Models.Local
{
    public class LocalScriptFile : LocalFile
    {
        public List<LocalSceneFile> Scenes { get; } = new List<LocalSceneFile>();

        public LocalScriptFile(string fullPath, string hash)
        : base(fullPath, hash)
        {
        }

        internal void ReferencedBy(LocalSceneFile scene)
        {
            if (!Scenes.Contains(scene))
            {
                Scenes.Add(scene);
            }
        }
    }
}
