using System.Collections.Generic;
using System.IO;

namespace Party.Shared
{
    public class SavesScanner
    {
        public static IEnumerable<Resource> Scan(string savesDirectory)
        {
            savesDirectory = Path.GetFullPath(savesDirectory);
            var scenes = new List<Scene>();
            var scripts = new List<Script>();

            foreach (var file in Directory.EnumerateFiles(savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                switch (Path.GetExtension(file))
                {
                    case ".json":
                        yield return new Scene(VamLocation.Absolute(savesDirectory, file));
                        break;
                    case ".cs":
                        yield return new Script(VamLocation.Absolute(savesDirectory, file));
                        break;
                }
            }
        }
    }
}
