using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Party.Shared.Resources
{
    public class Scene : Resource
    {
        public List<Script> Scripts { get; } = new List<Script>();

        public Scene(string fullPath) : base(fullPath)
        {
        }

        internal void References(Script script)
        {
            Scripts.Add(script);
        }
    }
}
