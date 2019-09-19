using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Utils;

namespace Party.Shared.Serializers
{
    public class ScriptListSerializer : IScriptListSerializer
    {
        private readonly IFileSystem _fs;
        private readonly Throttler _throttler;

        public ScriptListSerializer(IFileSystem fs, Throttler throttler)
        {
            _fs = fs ?? throw new System.ArgumentNullException(nameof(fs));
            _throttler = throttler ?? throw new System.ArgumentNullException(nameof(throttler));
        }

        public async Task<string[]> GetScriptsAsync(string fullPath)
        {
            string[] lines;
            using (await _throttler.ThrottleIO().ConfigureAwait(false))
            {
                lines = _fs.File.ReadAllLines(fullPath);
            }
            var paths = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            return paths;
        }
    }

    public interface IScriptListSerializer
    {
        Task<string[]> GetScriptsAsync(string fullPath);
    }
}
