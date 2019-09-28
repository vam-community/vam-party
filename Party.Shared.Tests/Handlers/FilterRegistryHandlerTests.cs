using System.Linq;
using NUnit.Framework;
using Party.Shared.Models;

namespace Party.Shared.Handlers
{
    public class FilterRegistryHandlerTests
    {
        private FilterRegistryHandler _handler;

        [SetUp]
        public void BeforeEach()
        {
            _handler = new FilterRegistryHandler(new[] { "https://example.org" });
        }

        [Test]
        public void CanWorkWithoutQueryNorScenes()
        {
            var script1 = TestFactory.RegScript("script1", TestFactory.RegVer("1.0.0", TestFactory.RegFile("My Script.cs", "12345", "https://example.org/scripts/MyScript.cs")));
            var registry = TestFactory.Reg(script1);

            var result = _handler.FilterRegistry(registry, "");

            PartyAssertions.AreDeepEqual(new[]
            {
                new SearchResult
                {
                    Package = script1,
                    Trusted = true
                }
            }, result);
        }

        [Test]
        public void CanFlagUntrustedDownloads()
        {
            var script1 = TestFactory.RegScript("script1", TestFactory.RegVer("1.0.0", TestFactory.RegFile("My Script.cs", "12345", "https://example.com/scripts/MyScript.cs")));
            var registry = TestFactory.Reg(script1);

            var result = _handler.FilterRegistry(registry, "");

            PartyAssertions.AreDeepEqual(new[]
            {
                new SearchResult
                {
                    Package = script1,
                    Trusted = false
                }
            }, result);
        }

        [TestCase("Script2")]
        [TestCase("john")]
        [TestCase("boom")]
        [TestCase("magic")]
        public void CanFilterScriptsByKeywords(string query)
        {
            var script1 = TestFactory.RegScript("script1", TestFactory.RegVer("1.0.0", TestFactory.RegFile("My Script.cs", "12345", "https://example.org/scripts/MyScript.cs")));
            var script2 = TestFactory.RegScript("script2", TestFactory.RegVer("1.0.0", TestFactory.RegFile("Super Stuff.cs", "67890", "https://example.org/scripts/Super Stuff.cs")));
            script2.Tags = new[] { "magic" }.ToList();
            script2.Author = "John Doe";
            script2.Description = "This is a script that makes stuff go boom!";
            var registry = TestFactory.Reg(script1, script2);

            var result = _handler.FilterRegistry(registry, query);

            PartyAssertions.AreDeepEqual(new[]
            {
                new SearchResult
                {
                    Package = script2,
                    Trusted = true
                }
            }, result);
        }
    }
}
