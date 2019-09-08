using System.Collections.Generic;
using Party.Shared.Resources;

namespace Party.Shared.Models
{
    public class SavesMap
    {
        public Script[] Scripts { get; set; }
        public Scene[] Scenes { get; set; }
        public SavesError[] Errors { get; set; }
    }

    public class SavesError
    {
        public string File { get; }
        public string Error { get; }

        public SavesError(string file, string error)
        {
            File = file;
            Error = error;
        }
    }
}
