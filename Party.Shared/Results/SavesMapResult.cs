using System.Collections.Generic;
using System.Linq;
using Party.Shared.Resources;

namespace Party.Shared.Results
{
    public class SavesMapResult
    {
        public IReadOnlyDictionary<string, Script> IdentifierScriptMap { get; internal set; }
        public Script[] Scripts { get => IdentifierScriptMap.Values.ToArray(); }
        public Scene[] Scenes { get; internal set; }
    }
}
