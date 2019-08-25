using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Handlers;
using Party.Shared.Resources;

namespace Party.Shared
{
    public class SavesResolverHandlerTests
    {

        [Test]
        public async Task CanIgnoreByExtension()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Scene 1.exe", new MockFileData("I'm bad") },
            });
            var handler = new SavesResolverHandler(fileSystem, @"C:\VaM\Saves", new string[0]);

            var result = await handler.AnalyzeSaves();

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap().Build(),
                result
            );
        }

        [Test]
        public async Task CanIgnoreByPath()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Ignored\Scene 1.json", new MockFileData("I don't count") },
            });
            var handler = new SavesResolverHandler(fileSystem, @"C:\VaM\Saves", new[] { @"C:\VaM\Saves\Ignored" });

            var result = await handler.AnalyzeSaves();

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap().Build(),
                result
            );
        }

        [Test]
        public async Task CanListScenesAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Scene 1.json", new MockFileData("{}") },
            });
            var handler = new SavesResolverHandler(fileSystem, @"C:\VaM\Saves", new string[0]);

            var result = await handler.AnalyzeSaves();

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap()
                    .WithScene(new Scene(@"C:\VaM\Saves\Scene 1.json"))
                    .Build(),
                result
            );
        }

        [Test]
        public async Task CanListScriptsAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Script 1.cs", new MockFileData("using Unity;\npublic class MyScript {}") },
            });
            var handler = new SavesResolverHandler(fileSystem, @"C:\VaM\Saves", new string[0]);

            var result = await handler.AnalyzeSaves();

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap()
                    .WithScript(new Script(@"C:\VaM\Saves\Script 1.cs", "90A449A3FC7A01DCF27C92090C05804BFF1EC887006A77F71E984D21F7B38CD4"), out var _)
                    .Build(),
                result
            );
        }

        [Test]
        public async Task CanFindScenesReferencingScriptsAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Scene 1.json", new MockFileData("{\"atoms\": [ { \"storables\": [ { \"id\": \"PluginManager\", \"plugins\": { \"plugin#0\": \"Saves/Script 1.cs\" } } ] } ] }") },
                { @"C:\VaM\Saves\Script 1.cs", new MockFileData("using Unity;\npublic class MyScript {}") }
            });
            var handler = new SavesResolverHandler(fileSystem, @"C:\VaM\Saves", new string[0]);

            var result = await handler.AnalyzeSaves();

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Script 1.cs" }));
            Assert.That(result.Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Scene 1.json" }));
            Assert.That(result.Scripts.First().Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Scene 1.json" }));
            Assert.That(result.Scenes.First().Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Script 1.cs" }));
        }
    }
}
