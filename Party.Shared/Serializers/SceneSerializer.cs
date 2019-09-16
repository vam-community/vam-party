using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Party.Shared.Exceptions;

namespace Party.Shared.Serializers
{
    public class SceneSerializer : ISceneSerializer
    {
        private readonly IFileSystem _fs;

        public SceneSerializer(IFileSystem fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public async Task<ISceneJson> Deserialize(string path)
        {
            try
            {
                using var file = _fs.File.OpenText(path);
                using var reader = new JsonTextReader(file);
                var json = (JObject)await JToken.ReadFromAsync(reader).ConfigureAwait(false);
                return new SceneJson(json);
            }
            catch (JsonReaderException exc)
            {
                throw new SavesException(exc.Message, exc);
            }
        }

        public async Task Serialize(ISceneJson scene, string path)
        {
            if (!(scene is SceneJson json))
                throw new InvalidCastException("SceneSerializer expects a SceneJson");

            using var file = _fs.File.CreateText(path);
            using var writer = new SceneJsonTextWriter(file);
            await json.Json.WriteToAsync(writer);
        }
    }

    public interface ISceneSerializer
    {
        Task<ISceneJson> Deserialize(string path);
        Task Serialize(ISceneJson scene, string path);
    }
}
