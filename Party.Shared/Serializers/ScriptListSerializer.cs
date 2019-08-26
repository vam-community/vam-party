using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Party.Shared.Serializers
{
    public class ScriptListSerializer
    {
        public Task<string[]> GetScriptsAsync(IFileSystem fs, string fullPath)
        {
            var lines = fs.File.ReadAllLines(fullPath);
            var paths = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            return Task.FromResult(paths);
        }
    }
}
