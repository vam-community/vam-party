using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class SceneTests
    {
        private Scene _scene;

        [SetUp]
        public void BeforeEach()
        {
            string sceneFile = Path.Combine(TestContext.GetTestsSavesDirectory(), "scene", "My Scene 1.json");
            _scene = new Scene(sceneFile);
        }

        [Test]
        public async Task CanListScripts()
        {
            var scripts = new List<Script>();
            await foreach (var script in _scene.GetScriptsAsync())
                scripts.Add(script);

            Assert.That(scripts.Select(scripts => scripts.Filename), Is.EquivalentTo(new[] { "My Script 1.cs" }));
        }
    }
}
