using System.Collections.Generic;
using System.Linq;
using Party.Shared.Resources;

namespace Party.Shared.Models
{
    public class SavesMap
    {
        public IReadOnlyDictionary<string, Script> ScriptsByFilename { get; set; }
        public Script[] Scripts { get => ScriptsByFilename.Values.Distinct().ToArray(); }
        public Scene[] Scenes { get; set; }
        public string[] Errors { get; set; }
    }
}
