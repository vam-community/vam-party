using System.Collections.Generic;
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
            var script1 = new RegistryResult.RegistryScript
            {
                Name = "script1",
                Versions = new List<RegistryResult.RegistryScriptVersion>
                {
                    new RegistryResult.RegistryScriptVersion
                    {
                        Version = "1.0",
                        Files = new List<RegistryResult.RegistryFile>
                        {
                            new RegistryResult.RegistryFile{
                                Filename = "MyScript.cs",
                                Hash = new RegistryResult.RegistryFileHash
                                {
                                    Value = "12345"
                                },
                                Url = "https://example.org/scripts/MyScript.cs"
                            }
                        }
                    }
                }
            };
            var registry = new RegistryResult
            {
                Scripts = new List<RegistryResult.RegistryScript>{
                    script1
                }
            };
            var saves = new SavesMapResult();

            var result = _handler.SearchAsync(registry, saves, "", false);

            PartyAssertions.AreDeepEqual(new SearchResult[]
            {
                new SearchResult
                {
                    Package = script1,
                    Trusted = true
                }
            }, result);
        }
    }
}
