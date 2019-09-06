using NUnit.Framework;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Resources;

namespace Party.Shared
{
    public class RegistrySavesMatchHandlerTests
    {
        private const string _vam = @"C:\VaM\Saves\";
        private RegistrySavesMatchHandler _handler;

        [SetUp]
        public void BeforeEach()
        {
            _handler = new RegistrySavesMatchHandler();
        }

        [Test]
        public void CanMatchNothing()
        {
            var saves = ResultFactory.SavesMap()
                .WithScript(new Script($"{_vam}1.cs", "1"), out var script1)
                .Build();
            var registry = ResultFactory.Reg(
                ResultFactory.RegScript("my-script", ResultFactory.RegVer("1.0.0", ResultFactory.RegFile("2.cs", "2"))));

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new RegistrySavesMatch[0],
                FilenameMatches = new RegistrySavesMatch[0],
                NoMatches = new[] { script1 }
            }, result);
        }

        [Test]
        public void CanMatchByHash()
        {
            var saves = ResultFactory.SavesMap()
                .WithScript(new Script($"{_vam}1.cs", "1"), out var local1)
                .Build();
            RegistryFile file = ResultFactory.RegFile("2.cs", "1");
            RegistryScriptVersion version = ResultFactory.RegVer("1.0.0", file);
            RegistryScript script = ResultFactory.RegScript("my-script", version);
            var registry = ResultFactory.Reg(script);

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new[] { new RegistrySavesMatch { Script = script, Version = version, File = file, Local = local1 } },
                FilenameMatches = new RegistrySavesMatch[0],
                NoMatches = new Script[0]
            }, result);
        }

        [Test]
        public void CanMatchByFilename()
        {
            var saves = ResultFactory.SavesMap()
                .WithScript(new Script($"{_vam}1.cs", "1"), out var local1)
                .Build();
            RegistryFile file = ResultFactory.RegFile("1.cs", "2");
            RegistryScriptVersion version = ResultFactory.RegVer("1.0.0", file);
            RegistryScript script = ResultFactory.RegScript("my-script", version);
            var registry = ResultFactory.Reg(script);

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new RegistrySavesMatch[0],
                FilenameMatches = new[] { new RegistrySavesMatch { Script = script, Version = version, File = file, Local = local1 } },
                NoMatches = new Script[0]
            }, result);
        }

        [Test]
        public void DoesNotMatchByFilenameWhenMatchedByHash()
        {
            var saves = ResultFactory.SavesMap()
                .WithScript(new Script($"{_vam}1.cs", "1"), out var local1)
                .Build();
            RegistryFile file = ResultFactory.RegFile("1.cs", "1");
            RegistryScriptVersion version = ResultFactory.RegVer("1.0.0", file);
            RegistryScript script = ResultFactory.RegScript("my-script", version);
            var registry = ResultFactory.Reg(script);

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new[] { new RegistrySavesMatch { Script = script, Version = version, File = file, Local = local1 } },
                FilenameMatches = new RegistrySavesMatch[0],
                NoMatches = new Script[0]
            }, result);
        }
    }
}
