using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Serializers;

namespace Party.Shared
{
    public class SceneSerializerTests
    {
        [Test]
        public async Task CanGetScriptsFromScene()
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

            var scripts = await new SceneSerializer(fileSystem).GetScriptsAsync(@"C:\VaM\Saves\Scene 1.json");

            Assert.That(scripts, Is.EqualTo(new[] { "Saves/Script 1.cs" }));
        }
    }
}
