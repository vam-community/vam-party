using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Models.Registries;
using Party.Shared.Serializers;

namespace Party.Shared
{
    public class SceneUpdateHandlerTests
    {
        [Test]
        public async Task CanFindAndReplaceAScript()
        {
            var scene = new Scene(@"C:\VaM\Saves\My Scene.json");
            var script = new Script(@"C:\VaM\Saves\My Script.cs", "SOMEHASH");
            var info = new LocalPackageInfo
            {
                Files = new[]
                {
                    new LocalPackageInfo.InstalledFileInfo
                    {
                        RegistryFile = new RegistryFile
                        {
                            Hash = new RegistryHash
                            {
                                Value = "SOMEHASH"
                            }
                        },
                        Path = @"C:\VaM\Saves\party\some-package\1.0.0\My Script.cs"
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
            serializer
                .Setup(s => s.UpdateScriptAsync(@"C:\VaM\Saves\My Scene.json", updates))
                .ReturnsAsync(effectiveUpdates);
            var handler = new SceneUpdateHandler(serializer.Object, @"C:\VaM\Saves");

            var result = await handler.UpdateScripts(scene, script, info);

            Assert.That(result, Is.EqualTo(effectiveUpdates));
        }
    }
}
