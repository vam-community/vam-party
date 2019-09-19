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
        private const string ScenePath = @"C:\VaM\Saves\Scene 1.json";

        [Test]
        public async Task CanDeserialize()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     ScenePath,
                     new MockFileData(GetSampleSceneJson())},
            });

            var scene = await new SceneSerializer(fileSystem, new Throttler()).DeserializeAsync(ScenePath);

            Assert.That(scene.Atoms.SelectMany(a => a.Plugins).Select(p => p.Path), Is.EqualTo(new[] { "Saves/Script 1.cs" }));
        }

        [Test]
        public async Task CanDeserializeAndReserializeAsIs()
        {
            string sceneJson = GetSampleSceneJson();
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     ScenePath,
                     new MockFileData(sceneJson)},
            });

            var serializer = new SceneSerializer(fileSystem, new Throttler());
            var scene = await serializer.DeserializeAsync(ScenePath);
            await serializer.SerializeAsync(scene, ScenePath);

            Assert.That(fileSystem.File.ReadAllText(ScenePath), Is.EqualTo(sceneJson));
        }

        [Test]
        public async Task CanGetScriptsFast()
        {
            string sceneJson = GetSampleSceneJson();
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     ScenePath,
                     new MockFileData(sceneJson)},
            });

            var serializer = new SceneSerializer(fileSystem, new Throttler());
            var scripts = await serializer.FindScriptsFastAsync(ScenePath);

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
