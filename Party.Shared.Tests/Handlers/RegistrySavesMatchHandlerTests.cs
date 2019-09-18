using NUnit.Framework;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;

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
                .WithScript(new LocalScriptFile($"{_vam}1.cs", "1"), out var script1)
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
                .WithScript(new LocalScriptFile($"{_vam}1.cs", "1"), out var local1)
                .Build();
            var file = ResultFactory.RegFile("2.cs", "1");
            var version = ResultFactory.RegVer("1.0.0", file);
            var package = ResultFactory.RegScript("my-script", version);
            var registry = ResultFactory.Reg(package);

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new[] { new RegistrySavesMatch { Remote = new RegistryPackageFileContext(registry, package, version, file), Local = local1 } },
                FilenameMatches = new RegistrySavesMatch[0],
                NoMatches = new LocalScriptFile[0]
            }, result);
        }

        [Test]
        public void CanMatchByFilename()
        {
            var saves = ResultFactory.SavesMap()
                .WithScript(new LocalScriptFile($"{_vam}1.cs", "1"), out var local1)
                .Build();
            var file = ResultFactory.RegFile("1.cs", "2");
            var version = ResultFactory.RegVer("1.0.0", file);
            var package = ResultFactory.RegScript("my-script", version);
            var registry = ResultFactory.Reg(package);

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new RegistrySavesMatch[0],
                FilenameMatches = new[] { new RegistrySavesMatch { Remote = new RegistryPackageFileContext(registry, package, version, file), Local = local1 } },
                NoMatches = new LocalScriptFile[0]
            }, result);
        }

        [Test]
        public void DoesNotMatchByFilenameWhenMatchedByHash()
        {
            var saves = ResultFactory.SavesMap()
                .WithScript(new LocalScriptFile($"{_vam}1.cs", "1"), out var local1)
                .Build();
            var file = ResultFactory.RegFile("1.cs", "1");
            var version = ResultFactory.RegVer("1.0.0", file);
            var package = ResultFactory.RegScript("my-script", version);
            var registry = ResultFactory.Reg(package);

            var result = _handler.Match(saves, registry);

            PartyAssertions.AreDeepEqual(new RegistrySavesMatches
            {
                HashMatches = new[] { new RegistrySavesMatch { Remote = new RegistryPackageFileContext(registry, package, version, file), Local = local1 } },
                FilenameMatches = new RegistrySavesMatch[0],
                NoMatches = new LocalScriptFile[0]
            }, result);
        }
    }
}
