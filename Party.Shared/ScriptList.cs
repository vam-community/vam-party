using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Party.Shared
{
    public class ScriptList : Resource
    {
        public override string Type { get => "cslist"; }

        public ScriptList(VamLocation path, IHashCache cache)
        : base(path, cache)
        {
        }

        public async IAsyncEnumerable<Script> GetScriptsAsync()
        {
            var lines = await File.ReadAllLinesAsync(Location.FullPath);
            foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                yield return new Script(VamLocation.RelativeTo(Location, line), Cache);
            }
        }
    }
}
