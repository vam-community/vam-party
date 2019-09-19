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
        [Test]
        public async Task CanDeserialize()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     @"C:\VaM\Saves\Scene 1.json",
                     new MockFileData(string.Join("\n", new[]
                        {
                            "{",
                            "  'atoms': [",
                            "    {",
                            "      'storables': [",
                            "        {",
                            "          'id': 'PluginManager',",
                            "          'plugins': {",
                            "            'plugin#0': 'Saves/Script 1.cs'",
                            "          }",
                            "        }",
                            "      ]",
                            "    }",
                            "  ]",
                            "}"
                        }.Select(line => line.Replace('\'', '"'))
                ))},
            });

            var scene = await new SceneSerializer(fileSystem, new Throttler()).Deserialize(@"C:\VaM\Saves\Scene 1.json");

            Assert.That(scene.Atoms.SelectMany(a => a.Plugins).Select(p => p.Path), Is.EqualTo(new[] { "Saves/Script 1.cs" }));
        }
    }
}
