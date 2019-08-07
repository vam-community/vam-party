using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Resources;

namespace Party.Shared.Tests.Resources
{
    public class SceneTests
    {
        private Scene _scene;

        [SetUp]
        public void BeforeEach()
        {
            _scene = new Scene(TestContext.GetSavesFile("scene", "My Scene 1.json"), new NoHashCache());
        }

        [Test]
        public async Task CanListScripts()
        {
            var scripts = new List<Script>();
            await foreach (var script in _scene.GetScriptsAsync())
                scripts.Add(script);

            Assert.That(scripts.Select(scripts => scripts.Location.RelativePath), Is.EquivalentTo(new[] { @"Scripts\My Script 1.cs".OnWindows() }));
        }
    }
}
