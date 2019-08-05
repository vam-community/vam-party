using System.Linq;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class SavesScannerTests
    {
        [Test]
        public void CanListAllResources()
        {
            var resources = SavesScanner.Scan(TestContext.GetTestsSavesDirectory()).ToList();

            Assert.That(resources.OfType<Scene>().Select(r => r.Location.RelativePath), Is.EquivalentTo(new[] { @"scene\My Scene 1.json" }));
            Assert.That(resources.OfType<Script>().Select(r => r.Location.RelativePath), Is.EquivalentTo(new[] { @"Scripts\My Script 1.cs" }));
        }
    }
}
