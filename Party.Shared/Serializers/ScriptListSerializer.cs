using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Party.Shared.Serializers
{
    public class ScriptListSerializer : IScriptListSerializer
    {
        private readonly IFileSystem _fs;

        public ScriptListSerializer(IFileSystem fs)
        {
            _fs = fs ?? throw new System.ArgumentNullException(nameof(fs));
        }

        public Task<string[]> GetScriptsAsync(string fullPath)
        {
            var lines = _fs.File.ReadAllLines(fullPath);
            var paths = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            return Task.FromResult(paths);
        }
    }

    public interface IScriptListSerializer
    {
        Task<string[]> GetScriptsAsync(string fullPath);
    }
}
