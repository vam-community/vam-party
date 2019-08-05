using System.Linq;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class SavesScannerTests
    {
        private Saves _saves;

        [SetUp]
        public void BeforeEach()
        {
            _saves = SavesScanner.Scan(TestContext.GetTestsSavesDirectory());
        }

        [Test]
        public void CanListScenes()
        {
            Assert.That(_saves.Scenes.Select(scenes => scenes.Filename), Is.EquivalentTo(new[] { "My Scene 1.json" }));
        }

        [Test]
        public void CanListScripts()
        {
            Assert.That(_saves.Scripts.Select(scenes => scenes.Filename), Is.EquivalentTo(new[] { "Some Script 1.cs" }));
        }
    }
}
