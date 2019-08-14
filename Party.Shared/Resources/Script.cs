using System.Collections.Generic;

namespace Party.Shared.Resources
{
    public class Script : Resource
    {
        public List<Scene> Scenes { get; } = new List<Scene>();

        public Script(string fullPath, string hash)
        : base(fullPath, hash)
        {
        }

        internal void ReferencedBy(Scene scene)
        {
            if (!Scenes.Contains(scene))
            {
                Scenes.Add(scene);
            }
        }
    }
}
