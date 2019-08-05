using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Party.Shared
{
    public class Scene : Resource
    {
        private async Task<JObject> ParseAsync()
        {
            using (var file = File.OpenText(FullPath))
            using (var reader = new JsonTextReader(file))
            {
                return (JObject)await JToken.ReadFromAsync(reader);
            }
        }

        public async IAsyncEnumerable<Script> GetScriptsAsync()
        {
            var json = await ParseAsync();
            var atoms = (JArray)json["atoms"];
            foreach (var atom in atoms)
            {
                var storables = (JArray)atom["storables"];
                foreach (var storable in storables)
                {
                    if ((string)storable["id"] == "PluginManager")
                    {
                        var plugins = (JObject)storable["plugins"];
                        foreach (var plugin in plugins.Properties())
                        {
                            yield return new Script(Path.GetFullPath(Path.Combine(ContainingDirectory, (string)plugin.Value)));
                        }
                    }
                }
            }
        }

        public Scene(string file)
        : base(file)
        {
        }
    }
}
