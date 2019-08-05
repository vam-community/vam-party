using System.Collections.Generic;
using System.IO;

namespace Party.Shared
{
    public class SavesScanner
    {
        public static Saves Scan(string savesDirectory)
        {
            var scenes = new List<Scene>();
            var scripts = new List<Script>();

            foreach (var file in Directory.EnumerateFiles(savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                switch (Path.GetExtension(file))
                {
                    case ".json":
                        scenes.Add(new Scene(VamLocation.Absolute(savesDirectory, file)));
                        break;
                    case ".cs":
                        scripts.Add(new Script(VamLocation.Absolute(savesDirectory, file)));
                        break;
                }
            }

            return new Saves(scenes.ToArray(), scripts.ToArray());
        }
    }
}
