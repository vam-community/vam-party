using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class ScriptTests
    {
        private Script _script;

        [SetUp]
        public void BeforeEach()
        {
            _script = new Script(TestContext.GetSavesFile("Scripts", "My Script 1.cs"), new NoScriptHashCache());
        }

        [Test]
        public void CanGetHash()
        {
            Assert.That(_script.GetHash(), Is.EqualTo("15D3CB7AF9BDE6ACF8ACE3DA97B0B43215DC2758B4C80B176D911F5AF2489D6D"));
        }
    }
}
