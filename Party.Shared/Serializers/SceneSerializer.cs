using System;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Party.Shared.Exceptions;
using Party.Shared.Utils;

namespace Party.Shared.Serializers
{
    public class SceneSerializer : ISceneSerializer
    {
        private static readonly Regex _findScriptsFastRegex = new Regex(
            "\"plugin#[0-9]{1,3}\" ?: ?\"(?<path>[^\"]{4,512})\"",
            RegexOptions.Compiled | RegexOptions.Multiline,
            TimeSpan.FromSeconds(5));

        private readonly IFileSystem _fs;
        private readonly Throttler _throttler;

        public SceneSerializer(IFileSystem fs, Throttler throttler)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _throttler = throttler ?? throw new ArgumentNullException(nameof(throttler));
        }

        public async Task<ISceneJson> DeserializeAsync(string path)
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

        public async Task SerializeAsync(ISceneJson scene, string path)
        {
            if (!(scene is SceneJson json))
                throw new InvalidCastException("SceneSerializer expects a SceneJson");

            using (await _throttler.ThrottleIO())
            {
                using var file = _fs.File.CreateText(path);
                using var writer = new SceneJsonTextWriter(file);
                await json.Json.WriteToAsync(writer);
            }
        }

        public async Task<string[]> FindScriptsFastAsync(string path)
        {
            string content;
            using (await _throttler.ThrottleIO())
            {
                content = _fs.File.ReadAllText(path);
            }

            var result = _findScriptsFastRegex.Matches(content);
            if (result == null) return new string[0];
            return result.Cast<Match>().Select(m => m.Groups["path"].Value).ToArray();
        }
    }

    public interface ISceneSerializer
    {
        Task<ISceneJson> DeserializeAsync(string path);
        Task SerializeAsync(ISceneJson scene, string path);
        Task<string[]> FindScriptsFastAsync(string path);
    }
}
