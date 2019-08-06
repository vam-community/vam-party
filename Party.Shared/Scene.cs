using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Party.Shared
{
    public class Scene : Resource
    {
        public Scene(VamLocation path, IHashCache cache)
        : base(path, cache)
        {
        }

        public async IAsyncEnumerable<Script> GetScriptsAsync()
        {
            var json = await ParseAsync();
            var atoms = (JArray)json["atoms"];
            if (atoms == null) yield break;
            foreach (var atom in atoms)
            {
                var storables = (JArray)atom["storables"];
                if (storables == null) continue;
                foreach (var storable in storables)
                {
                    if ((string)storable["id"] == "PluginManager")
                    {
                        var plugins = (JObject)storable["plugins"];
                        if (plugins == null) continue;
                        foreach (var plugin in plugins.Properties())
                        {
                            yield return new Script(VamLocation.RelativeTo(Location, (string)plugin.Value), Cache);
                        }
                    }
                }
            }
        }

        private async Task<JObject> ParseAsync()
        {
            using (var file = File.OpenText(Location.FullPath))
            using (var reader = new JsonTextReader(file))
            {
                return (JObject)await JToken.ReadFromAsync(reader);
            }
        }
    }
}
