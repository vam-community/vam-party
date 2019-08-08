using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Resources;

namespace Party.Shared.Tests.Resources
{
    public class ScriptTests
    {
        private Script _script;

        [SetUp]
        public void BeforeEach()
        {
            _script = new Script(TestContext.GetSavesFile("Scripts", "My Script 1.cs"), new NoHashCache());
        }

        [Test]
        public async Task CanGetHash()
        {
            string hash = await _script.GetHashAsync();

            Assert.That(hash, Is.EqualTo("7C656425A97C2581C29357D5F181EF916A484D57C31E6B57F24C969AC5FF4CA7"));
        }
    }
}
