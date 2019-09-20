using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;
using Party.Shared.Serializers;

namespace Party.Shared
{
    public class ApplyNormalizedPathsToSceneHandlerTests
    {
        [Test]
        public async Task CanFindAndReplaceAScript()
        {
            var scene = new LocalSceneFile(@"C:\VaM\Saves\My Scene.json");
            var script = new LocalScriptFile(@"C:\VaM\Saves\My Script.cs", "SOMEHASH");
            var info = new LocalPackageInfo
            {
                Files = new[]
                {
                    new InstalledFileInfo
                    {
                        RegistryFile = new RegistryFile
                        {
                            Hash = new RegistryHash
                            {
                                Value = "SOMEHASH"
                            }
                        },
                        FullPath = @"C:\VaM\Saves\party\some-package\1.0.0\My Script.cs"
                    }
                }
            };
            var serializer = new Mock<ISceneSerializer>(MockBehavior.Strict);
            var updates = new List<(string before, string after)>{
                (@"Saves/My Script.cs", @"Saves/party/some-package/1.0.0/My Script.cs"),
                (@"My Script.cs", @"Saves/party/some-package/1.0.0/My Script.cs")
            };
            var effectiveUpdates = new List<(string before, string after)>{
                (@"Saves/My Script.cs", @"Saves/party/some-package/1.0.0/My Script.cs")
            };
            var json = new SceneJsonMock(new AtomJsonMock(new PluginJsonMock(@"Saves/My Script.cs")));
            serializer
                .Setup(s => s.DeserializeAsync(@"C:\VaM\Saves\My Scene.json"))
                .ReturnsAsync(json);
            serializer
                .Setup(s => s.SerializeAsync(json, @"C:\VaM\Saves\My Scene.json"))
                .Returns(Task.CompletedTask);
            var handler = new ApplyNormalizedPathsToSceneHandler(serializer.Object, @"C:\VaM");

            var result = await handler.ApplyNormalizedPathsToSceneAsync(scene, script, info);

            Assert.That(result, Is.EqualTo(effectiveUpdates));
        }
    }
}
