using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Serializers;
using Party.Shared.Utils;

namespace Party.Shared
{
    public class ScanLocalFilesHandlerTests
    {
        [Test]
        public async Task CanIgnoreByExtension()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Scene 1.exe", new MockFileData("I'm bad") },
            });
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

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
            var handler = Create(fileSystem, new[] { @"C:\VaM\Saves\Ignored" });

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

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
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap()
                    .WithScene(new LocalSceneFile(@"C:\VaM\Saves\Scene 1.json"))
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
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap()
                    .WithScript(new LocalScriptFile(@"C:\VaM\Saves\Script 1.cs", "90A449A3FC7A01DCF27C92090C05804BFF1EC887006A77F71E984D21F7B38CD4"), out var _)
                    .Build(),
                result
            );
        }

        [Test]
        public async Task CanFindScenesReferencingScriptsAbsoluteAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Scene 1.json", new MockFileData("{\"atoms\": [ { \"storables\": [ { \"id\": \"PluginManager\", \"plugins\": { \"plugin#0\": \"Saves/Script 1.cs\" } } ] } ] }") },
                { @"C:\VaM\Saves\Script 1.cs", new MockFileData("using Unity;\npublic class MyScript {}") }
            });
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

            AssertNoErrors(result);
            Assert.That(result.Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Script 1.cs" }));
            Assert.That(result.Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Scene 1.json" }));
            Assert.That(result.Scripts.First().Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Scene 1.json" }));
            Assert.That(result.Scenes.First().Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Script 1.cs" }));
        }

        [Test]
        public async Task CanFindScenesReferencingScriptsRelativeAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Downloads\Scene 1\Scene 1.json", new MockFileData("{\"atoms\": [ { \"storables\": [ { \"id\": \"PluginManager\", \"plugins\": { \"plugin#0\": \"Script 1.cs\" } } ] } ] }") },
                { @"C:\VaM\Saves\Downloads\Scene 1\Script 1.cs", new MockFileData("using Unity;\npublic class MyScript {}") }
            });
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

            AssertNoErrors(result);
            Assert.That(result.Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Script 1.cs" }));
            Assert.That(result.Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Scene 1.json" }));
            Assert.That(result.Scripts.First().Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Scene 1.json" }));
            Assert.That(result.Scenes.First().Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Script 1.cs" }));
        }

        [Test]
        public async Task CanListScriptListsAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\My Script\Script 1.cs", new MockFileData("using Unity;\npublic class MyScript {}") },
                { @"C:\VaM\Saves\My Script\Add Me.cslist", new MockFileData("Script 1.cs") },
            });
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

            PartyAssertions.AreDeepEqual(
                ResultFactory.SavesMap()
                    .WithScript(new LocalScriptListFile(@"C:\VaM\Saves\My Script\Add Me.cslist", "3258C0B1D41C29CBC98B475EEEB5BF7609C9B4F290168A0E2158253DF044F325", new[] {
                        new LocalScriptFile(@"C:\VaM\Saves\My Script\Script 1.cs", "90A449A3FC7A01DCF27C92090C05804BFF1EC887006A77F71E984D21F7B38CD4")
                    }), out var _)
                    .Build(),
                result
            );
        }

        [Test]
        public async Task CanFindScenesReferencingScriptListsRelativeAsync()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\VaM\Saves\Downloads\Scene 1\Scene 1.json", new MockFileData("{\"atoms\": [ { \"storables\": [ { \"id\": \"PluginManager\", \"plugins\": { \"plugin#0\": \"Add Me.cslist\" } } ] } ] }") },
                { @"C:\VaM\Saves\Downloads\Scene 1\Script 1.cs", new MockFileData("using Unity;\npublic class MyScript {}") },
                { @"C:\VaM\Saves\Downloads\Scene 1\Add Me.cslist", new MockFileData("Script 1.cs") },
            });
            var handler = Create(fileSystem);

            var result = await handler.ScanLocalFilesAsync(null, new ProgressMock<ScanLocalFilesProgress>());

            AssertNoErrors(result);
            Assert.That(result.Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Add Me.cslist" }));
            Assert.That(result.Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Scene 1.json" }));
            Assert.That(result.Scripts.First().Scenes.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Scene 1.json" }));
            Assert.That(result.Scenes.First().Scripts.Select(s => s.FullPath), Is.EquivalentTo(new[] { @"C:\VaM\Saves\Downloads\Scene 1\Add Me.cslist" }));
        }

        private ScanLocalFilesHandler Create(IFileSystem fileSystem, string[] ignoredPaths = null)
        {
            var throttler = new Throttler();
            return new ScanLocalFilesHandler(
                fileSystem,
                new SceneSerializer(fileSystem, throttler),
                new ScriptListSerializer(fileSystem, throttler),
                @"C:\VaM",
                ignoredPaths ?? new string[0]);
        }

        private void AssertNoErrors(SavesMap result)
        {
            Assert.That(result.Scenes.SelectMany(s => s.Errors ?? new List<LocalFileError>()), Is.Empty);
            Assert.That(result.Scripts.SelectMany(s => s.Errors ?? new List<LocalFileError>()), Is.Empty);
        }
    }
}
