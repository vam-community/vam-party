using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Party.Shared.Exceptions;
using Party.Shared.Utils;

namespace Party.Shared.Serializers
{
    public class SceneSerializer : ISceneSerializer
    {
        private readonly IFileSystem _fs;
        private readonly Throttler _throttler;

        public SceneSerializer(IFileSystem fs, Throttler throttler)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _throttler = throttler ?? throw new ArgumentNullException(nameof(throttler));
        }

        public async Task<ISceneJson> Deserialize(string path)
        {
            try
            {
                JToken json;
                using (await _throttler.ThrottleIO().ConfigureAwait(false))
                {
                    using var file = _fs.File.OpenText(path);
                    using var reader = new JsonTextReader(file);
                    json = await JToken.ReadFromAsync(reader).ConfigureAwait(false);
                }
                return new SceneJson((JObject)json);
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
