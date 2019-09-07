using System.Collections.Generic;
using Party.Shared.Resources;

namespace Party.Shared.Models
{
    public class SavesMap
    {
        public Script[] Scripts { get; set; }
        public Scene[] Scenes { get; set; }
        public (string file, string error)[] Errors { get; set; }
    }
}
