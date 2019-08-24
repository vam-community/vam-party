using System.Linq;
using NUnit.Framework;
using Party.Shared.Results;

namespace Party.Shared.Handlers
{
    public class SearchHandlerTests
    {
        private SearchHandler _handler;

        [SetUp]
        public void BeforeEach()
        {
            var config = new PartyConfiguration
            {
                Registry = new PartyConfigurationRegistry
                {
                    TrustedDomains = new[] { "https://example.org" }
                }
            };
            _handler = new SearchHandler(config);
        }

        [Test]
        public void CanWorkWithoutQueryNorScenes()
        {
            var script1 = ResultFactory.RegScript("script1", ResultFactory.RegVer("1.0", ResultFactory.RegFile("My Script.cs", "12345", "https://example.org/scripts/MyScript.cs")));
            var registry = ResultFactory.Reg(script1);
            var saves = new SavesMapResult();

            var result = _handler.Search(registry, saves, "", false);

            PartyAssertions.AreDeepEqual(new SearchResult[]
            {
                new SearchResult
                {
                    Package = script1,
                    Trusted = true
                }
            }, result);
        }

        [TestCase("Script2")]
        [TestCase("john")]
        [TestCase("boom")]
        [TestCase("magic")]
        public void CanFilterScriptsByKeywords(string query)
        {
            var script1 = ResultFactory.RegScript("script1", ResultFactory.RegVer("1.0", ResultFactory.RegFile("My Script.cs", "12345", "https://example.org/scripts/MyScript.cs")));
            var script2 = ResultFactory.RegScript("script2", ResultFactory.RegVer("1.0", ResultFactory.RegFile("Super Stuff.cs", "67890", "https://example.org/scripts/Super Stuff.cs")));
            script2.Tags = new[] { "magic" }.ToList();
            script2.Author = new RegistryScriptAuthor { Name = "John Doe" };
            script2.Description = "This is a script that makes stuff go boom!";
            var registry = ResultFactory.Reg(script1, script2);
            var saves = new SavesMapResult();

            var result = _handler.Search(registry, saves, query, false);

            PartyAssertions.AreDeepEqual(new SearchResult[]
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
