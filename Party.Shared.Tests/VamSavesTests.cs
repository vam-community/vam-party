using System.Linq;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class VamSavesTests
    {
        private VamSaves _saves;

        [SetUp]
        public void BeforeEach()
        {
            _saves = new VamSaves(TestContext.GetTestsSavesDirectory());
        }

        [Test]
        public void ListsAllScenes()
        {
            var scenes = _saves.GetAllScenes();

            Assert.That(scenes.Select(scenes => scenes.Filename), Is.EquivalentTo(new[] { "My Scene 1.json" }));
        }
    }
}
