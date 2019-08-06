using System.Linq;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class SavesScannerTests
    {
        [Test]
        public void CanListAllResources()
        {
            var resources = SavesScanner.Scan(TestContext.GetTestsSavesDirectory(), new string[0]).ToList();

            Assert.That(
                resources.OfType<Scene>().Select(r => r.Location.RelativePath),
                Is.EquivalentTo(new[]
                {
                    @"scene\My Scene 1.json",
                    @"scene\Complex Scene\Complex Scene 1.json"
                })
            );
            Assert.That(
                resources.OfType<Script>().Select(r => r.Location.RelativePath),
                Is.EquivalentTo(new[]
                {
                    @"Scripts\My Script 1.cs",
                    @"scene\Complex Scene\My Script 1.cs",
                    @"Scripts\Complex Plugin\Complex Script 1.cs",
                    @"Scripts\Complex Plugin\Complex Script 2.cs"
                })
            );
            Assert.That(
                resources.OfType<ScriptList>().Select(r => r.Location.RelativePath),
                Is.EquivalentTo(new[]
                {
                    @"Scripts\Complex Plugin\Complex Plugin.cslist"
                })
            );
        }
    }
}
