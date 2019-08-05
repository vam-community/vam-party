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
            var scenes = _saves.Scenes.Select(scene => scene.Location.RelativePath);

            Assert.That(scenes, Is.EquivalentTo(new[] { @"scene\My Scene 1.json" }));
        }

        [Test]
        public void CanListScripts()
        {
            var scripts = _saves.Scripts.Select(script => script.Location.RelativePath);

            Assert.That(scripts, Is.EquivalentTo(new[] { @"Scripts\My Script 1.cs" }));
        }
    }
}