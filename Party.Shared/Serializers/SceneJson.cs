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
        public ICollection<IPluginJson> Plugins { get; }

        public AtomJson(JToken json)
        {
            _json = json ?? throw new ArgumentNullException(nameof(json));
            Plugins = (((_json["storables"] as JArray)?.FirstOrDefault(storable => (string)storable["id"] == "PluginManager") as JObject)?["plugins"] as JObject)
                .Properties()
                .Select(plugin => new PluginJson(plugin))
                .Where(plugin => plugin?.Path != null)
                .ToList<IPluginJson>()
                ?? new List<IPluginJson>();
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
        public ICollection<IPluginJson> Plugins { get; }
    }

    public interface IPluginJson
    {
        public string Path { get; set; }
    }
}
