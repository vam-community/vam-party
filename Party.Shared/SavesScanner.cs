using System.Collections.Generic;
using System.IO;

namespace Party.Shared
{
    public class SavesScanner
    {
        public static Saves Scan(string directory)
        {
            var scenes = new List<Scene>();
            var scripts = new List<Script>();

            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                switch (Path.GetExtension(file))
                {
                    case ".json":
                        scenes.Add(new Scene(file));
                        break;
                    case ".cs":
                        scripts.Add(new Script(file));
                        break;
                }
            }

            return new Saves(scenes.ToArray(), scripts.ToArray());
        }
    }
}
