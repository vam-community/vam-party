using System;
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
                var scripts = new List<string>();
                ProcessScripts(json, script => { scripts.Add(script); return null; });
                return scripts.ToArray();
            }
            catch (JsonReaderException exc)
            {
                throw new SavesException($"There was an issue loading scene '{path}': {exc.Message}", exc);
            }
        }

        public async Task UpdateScriptAsync(IFileSystem fs, string path, List<(string before, string after)> updates)
        {
            try
            {
                var json = await LoadJson(fs, path).ConfigureAwait(false);
                ProcessScripts(json, script => updates.Where(u => u.before == script).Select(u => u.after).FirstOrDefault());
                using var file = fs.File.CreateText(@path);
                using JsonTextWriter writer = new JsonTextWriter(file)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 3,
                    // TODO: Formatting in VaM uses `"field" : "value"`, so we must figure out a way to add the space before the colon...
                };
                json.WriteTo(writer);
            }
            catch (JsonReaderException exc)
            {
                throw new SavesException($"There was an issue loading scene '{path}': {exc.Message}", exc);
            }
        }

        private void ProcessScripts(JObject json, Func<string, string> transform)
        {
            var atoms = (JArray)json["atoms"];
            if (atoms == null) { return; }
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
                            var newValue = transform(relativePath);
                            if (newValue != null && relativePath != newValue)
                            {
                                plugin.Value = newValue;
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
