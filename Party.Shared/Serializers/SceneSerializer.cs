using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Party.Shared.Exceptions;

namespace Party.Shared.Serializers
{
    public class SceneSerializer
    {
        public async Task<string[]> GetScriptsAsync(IFileSystem fs, string path)
        {
            try
            {
                var json = await LoadJson(fs, path).ConfigureAwait(false);
                return FindScriptsInJson(json).ToArray();
            }
            catch (JsonReaderException exc)
            {
                throw new SavesException($"There was an issue loading scene '{path}': {exc.Message}", exc);
            }
        }

        private IEnumerable<string> FindScriptsInJson(JObject json)
        {
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

        private async Task<JObject> LoadJson(IFileSystem fs, string path)
        {
            using var file = fs.File.OpenText(path);
            using var reader = new JsonTextReader(file);
            return (JObject)await JToken.ReadFromAsync(reader).ConfigureAwait(false);
        }
    }
}
