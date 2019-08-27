using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Party.Shared.Serializers
{
    public class SceneSerializer
    {
        public async IAsyncEnumerable<string> GetScriptsAsync(IFileSystem fs, string path)
        {
            var json = await ParseAsync(fs, path).ConfigureAwait(false);
            var atoms = (JArray)json["atoms"];
            if (atoms == null) { yield break; }
            foreach (var atom in atoms)
            {
                var storables = (JArray)atom["storables"];
                if (storables == null) { continue; }
                foreach (var storable in storables)
                {
                    if ((string)storable["id"] == "PluginManager")
                    {
                        var plugins = (JObject)storable["plugins"];
                        if (plugins == null) { continue; }
                        foreach (var plugin in plugins.Properties())
                        {
                            var relativePath = (string)plugin.Value;
                            if (relativePath != null)
                            {
                                yield return relativePath;
                            }
                        }
                    }
                }
            }
        }

        private async Task<JObject> ParseAsync(IFileSystem fs, string path)
        {
            using var file = fs.File.OpenText(path);
            using var reader = new JsonTextReader(file);
            return (JObject)await JToken.ReadFromAsync(reader).ConfigureAwait(false);
        }
    }
}
