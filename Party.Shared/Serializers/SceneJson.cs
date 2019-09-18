using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Party.Shared.Serializers
{
    public class SceneJson : ISceneJson
    {
        public ICollection<IAtomJson> Atoms { get; }
        internal JObject Json { get; }

        public SceneJson(JObject json)
        {
            Json = json ?? throw new ArgumentNullException(nameof(json));
            Atoms = (Json["atoms"] as JArray)?
            .Select(atom => new AtomJson(atom))
            .ToList<IAtomJson>()
            ?? new List<IAtomJson>();
        }
    }

    public class AtomJson : IAtomJson
    {
        private readonly JToken _json;
        public IEnumerable<IPluginJson> Plugins
        {
            get
            {
                if (!(_json["storables"] is JArray storablesJson))
                    return new PluginJson[0];
                var pluginsManager = storablesJson.FirstOrDefault(s => (string)s["id"] == "PluginManager");
                if (!(pluginsManager != null && pluginsManager is JObject pluginsManagerObj))
                    return new PluginJson[0];
                if (!(pluginsManagerObj["plugins"] is JObject pluginsObj))
                    return new PluginJson[0];
                return pluginsObj.Properties().Select(plugin => new PluginJson(plugin)).Where(plugin => plugin.Path != null);
            }
        }

        public AtomJson(JToken json)
        {
            _json = json ?? throw new ArgumentNullException(nameof(json));
        }
    }

    public class PluginJson : IPluginJson
    {
        private readonly JProperty _json;

        public PluginJson(JProperty json)
        {
            _json = json ?? throw new ArgumentNullException(nameof(json));
        }

        public string Path
        {
            get => (string)_json.Value;
            set => _json.Value = value;
        }
    }

    public interface ISceneJson
    {
        public ICollection<IAtomJson> Atoms { get; }
    }

    public interface IAtomJson
    {
        public IEnumerable<IPluginJson> Plugins { get; }
    }

    public interface IPluginJson
    {
        public string Path { get; set; }
    }
}
