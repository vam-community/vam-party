using System.Collections.Generic;
using System.IO;
using System.Linq;
using Party.Shared.Resources;

namespace Party.Shared.Discovery
{
    public class SavesScanner
    {
        private readonly string _savesDirectory;
        private readonly string[] _ignoredPaths;

        public SavesScanner(string savesDirectory, string[] ignoredPaths)
        {
            _savesDirectory = savesDirectory;
            _ignoredPaths = ignoredPaths;
        }

        public IEnumerable<Resource> Scan()
        {
            var cache = new HashCache();

            foreach (var file in Directory.EnumerateFiles(_savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                var path = VamLocation.Absolute(_savesDirectory, file);

                if (_ignoredPaths.Any(ignoredPath => path.RelativePath.StartsWith(ignoredPath))) continue;

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
                    default:
                        // Ignore anything else
                        break;
                }
            }
        }
    }
}
