using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Serializers;
using Party.Shared.Utils;

namespace Party.Shared
{
    public class SceneSerializerTests
    {
        private const string _scenePath = @"C:\VaM\Saves\Scene 1.json";

        [Test]
        public async Task CanDeserialize()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     _scenePath,
                     new MockFileData(GetSampleSceneJson())},
            });

            var scene = await new SceneSerializer(fileSystem, new Throttler()).DeserializeAsync(_scenePath);

            Assert.That(scene.Atoms.SelectMany(a => a.Plugins).Select(p => p.Path), Is.EqualTo(new[] { "Saves/Script 1.cs" }));
        }

        [Test]
        public async Task CanDeserializeAndReSerializeAsIs()
        {
            string sceneJson = GetSampleSceneJson();
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     _scenePath,
                     new MockFileData(sceneJson)},
            });

            var serializer = new SceneSerializer(fileSystem, new Throttler());
            var scene = await serializer.DeserializeAsync(_scenePath);
            await serializer.SerializeAsync(scene, _scenePath);

            Assert.That(fileSystem.File.ReadAllText(_scenePath), Is.EqualTo(sceneJson));
        }

        [Test]
        public async Task CanGetScriptsFast()
        {
            string sceneJson = GetSampleSceneJson();
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     _scenePath,
                     new MockFileData(sceneJson)},
            });

            var serializer = new SceneSerializer(fileSystem, new Throttler());
            var scripts = await serializer.FindScriptsFastAsync(_scenePath);

            Assert.That(scripts, Is.EqualTo(new[]
            {
                "Saves/Script 1.cs"
            }));
        }

        private static string GetSampleSceneJson()
        {
            return string.Join("\r\n", new[]
                {
                    "{ ",
                    "   'atoms' : [ ",
                    "      { ",
                    "         'storables' : [ ",
                    "            { ",
                    "               'id' : 'PluginManager', ",
                    "               'plugins' : { ",
                    "                  'plugin#0' : 'Saves/Script 1.cs'",
                    "               }",
                    "            }",
                    "         ]",
                    "      }",
                    "   ]",
                    "}"
                }.Select(line => line.Replace('\'', '"'))
            );
        }
    }
}
