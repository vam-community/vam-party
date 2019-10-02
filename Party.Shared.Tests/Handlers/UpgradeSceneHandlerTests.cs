using System.Collections.Generic;
using System.Linq;
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
    public class UpgradeSceneHandlerTests
    {
        [Test]
        public async Task CanFindAndReplaceAScript()
        {
            var folders = new Mock<IFoldersHelper>();
            folders
            .Setup(x => x.ToRelativeToVam(It.IsAny<string>()))
            .Returns((string path) => path.Replace(@"C:\VaM\", ""));
            var scene = new LocalSceneFile(@"C:\VaM\Saves\My Scene.json");
            var script = new LocalScriptFile(@"C:\VaM\Custom\Scripts\My Script.cs", "SOMEHASH");
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
                        FullPath = @"C:\VaM\Custom\Scripts\author\some-package\1.0.0\My Script.cs"
                    }
                }
            };
            var serializer = new Mock<ISceneSerializer>(MockBehavior.Strict);
            var updates = new List<(string before, string after)>{
                (@"Custom/Scripts/My Script.cs", @"Custom/Scripts/author/some-package/1.0.0/My Script.cs"),
                (@"Saves/Scripts/My Script.cs", @"Custom/Scripts/author/some-package/1.0.0/My Script.cs"),
                (@"My Script.cs", @"Custom/Scripts/author/some-package/1.0.0/My Script.cs")
            };
            var effectiveUpdates = new List<(string before, string after)>{
                (@"Custom/Scripts/My Script.cs", @"Custom/Scripts/author/some-package/1.0.0/My Script.cs")
            };
            var json = new SceneJsonMock(new AtomJsonMock(new PluginJsonMock(@"Custom/Scripts/My Script.cs")));
            serializer
                .Setup(s => s.DeserializeAsync(@"C:\VaM\Saves\My Scene.json"))
                .ReturnsAsync(json);
            serializer
                .Setup(s => s.SerializeAsync(json, @"C:\VaM\Saves\My Scene.json"))
                .Returns(Task.CompletedTask);
            var handler = new UpgradeSceneHandler(serializer.Object, folders.Object);

            var result = await handler.UpgradeSceneAsync(scene, script, info);

            Assert.That(result, Is.EqualTo(1));
            Assert.That(json.Atoms.First().Plugins.First().Path, Is.EqualTo("Custom/Scripts/author/some-package/1.0.0/My Script.cs"));
        }
    }
}
