using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
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
    }
}
