using System.Linq;
using NUnit.Framework;
using Party.Shared.Discovery;
using Party.Shared.Resources;

namespace Party.Shared.Tests.Discovery
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
                    @"scene\My Scene 1.json".OnWindows(),
                    @"scene\Complex Scene\Complex Scene 1.json".OnWindows()
                })
            );
            Assert.That(
                resources.OfType<Script>().Select(r => r.Location.RelativePath),
                Is.EquivalentTo(new[]
                {
                    @"Scripts\My Script 1.cs".OnWindows(),
                    @"scene\Complex Scene\My Script 1.cs".OnWindows(),
                    @"Scripts\Complex Plugin\Complex Script 1.cs".OnWindows(),
                    @"Scripts\Complex Plugin\Complex Script 2.cs".OnWindows()
                })
            );
            Assert.That(
                resources.OfType<ScriptList>().Select(r => r.Location.RelativePath),
                Is.EquivalentTo(new[]
                {
                    @"Scripts\Complex Plugin\Complex Plugin.cslist".OnWindows()
                })
            );
        }
    }
}
