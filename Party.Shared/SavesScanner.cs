using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Party.Shared
{
    public class SavesScanner
    {
        public static IEnumerable<Resource> Scan(string savesDirectory, string[] ignore)
        {
            savesDirectory = Path.GetFullPath(savesDirectory);
            var cache = new ScriptHashCache();
            var scenes = new List<Scene>();
            var scripts = new List<Script>();

            foreach (var file in Directory.EnumerateFiles(savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                var path = VamLocation.Absolute(savesDirectory, file);
                if (ignore.Any(i => path.RelativePath.StartsWith(i))) continue;
                switch (Path.GetExtension(file))
                {
                    case ".json":
                        yield return new Scene(path, cache);
                        break;
                    case ".cs":
                        yield return new Script(path, cache);
                        break;
                }
            }
        }
    }
}
