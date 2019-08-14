using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Party.Shared.Resources
{
    public class Scene : Resource
    {
        public List<Script> Scripts { get; } = new List<Script>();

        public Scene(string fullPath) : base(fullPath)
        {
        }

        internal void References(Script script)
        {
            Scripts.Add(script);
        }

        public async IAsyncEnumerable<string> GetScriptsAsync()
        {
            var json = await ParseAsync().ConfigureAwait(false);
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

        private async Task<JObject> ParseAsync()
        {
            using (var file = File.OpenText(FullPath))
            using (var reader = new JsonTextReader(file))
            {
                return (JObject)await JToken.ReadFromAsync(reader).ConfigureAwait(false);
            }
        }
    }
}
