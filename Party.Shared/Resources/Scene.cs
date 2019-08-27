using System.Collections.Generic;

namespace Party.Shared.Resources
{
    public class Scene : Resource
    {
        public List<Script> Scripts { get; } = new List<Script>();

        public Scene(string fullPath) : base(fullPath, null)
        {
        }

        internal void References(Script script)
        {
            Scripts.Add(script);
        }
    }
}
