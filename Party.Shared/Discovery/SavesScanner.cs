using System.Collections.Generic;
using System.IO;
using System.Linq;
using Party.Shared.Resources;

namespace Party.Shared.Discovery
{
    public class SavesScanner
    {
        public static IEnumerable<Resource> Scan(string savesDirectory, string[] ignoredPaths)
        {
            savesDirectory = Path.GetFullPath(savesDirectory);
            var cache = new HashCache();

            foreach (var file in Directory.EnumerateFiles(savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                var path = VamLocation.Absolute(savesDirectory, file);

                if (ignoredPaths.Any(ignoredPath => path.RelativePath.StartsWith(ignoredPath))) continue;

                switch (Path.GetExtension(file))
                {
                    case ".json":
                        yield return new Scene(path, cache);
                        break;
                    case ".cs":
                        yield return new Script(path, cache);
                        break;
                    case ".cslist":
                        yield return new ScriptList(path, cache);
                        break;
                }
            }
        }
    }
}
