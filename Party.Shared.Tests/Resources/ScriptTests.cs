using System;
using System.IO;
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
        public void CanGetHash()
        {
            string hash = _script.GetHash();

            switch (Environment.NewLine)
            {
                case "\r\n":
                    Assert.That(hash, Is.EqualTo("15D3CB7AF9BDE6ACF8ACE3DA97B0B43215DC2758B4C80B176D911F5AF2489D6D"));
                    break;
                case "\n":
                    Assert.That(hash, Is.EqualTo("E9AF9D630A723045CE7EB4C1219DF042BCEC2A3B740891F2F9D22B0E8D6FA156"));
                    break;
                default:
                    Assert.Fail("Unknown newline characters, cannot proceed with this test");
                    break;
            }
        }
    }
}
