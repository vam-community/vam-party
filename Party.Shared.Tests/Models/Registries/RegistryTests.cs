using NUnit.Framework;
using Party.Shared.Models.Registries;

namespace Party.Shared.Registries
{
    public class RegistryTests
    {
        [TestCase("package")]
        [TestCase("my-package")]
        [TestCase("package25")]
        public void CanValidateNames(string name)
        {
            Assert.That(RegistryPackage.ValidNameRegex.IsMatch(name), Is.True);
        }

        public void CanGetSpecificVersion()
        {
            RegistryPackageVersion version = TestFactory.RegVer("1.0.0");
            RegistryPackage package = TestFactory.RegScript("script1", version);
            var registry = TestFactory.Reg(package);

            var success = registry.TryGetPackageVersion(RegistryPackageType.Scripts, "script1", "1.0.0", out var context);

            Assert.That(success, Is.True);
            Assert.That(context.Registry, Is.SameAs(registry));
            Assert.That(context.Package, Is.SameAs(package));
            Assert.That(context.Version, Is.SameAs(version));
        }

        public void CanGetLatestVersion()
        {
            RegistryPackageVersion version = TestFactory.RegVer("1.0.0");
            RegistryPackage package = TestFactory.RegScript("script1", version);
            var registry = TestFactory.Reg(package);

            var success = registry.TryGetPackageVersion(RegistryPackageType.Scripts, "script1", null, out var context);

            Assert.That(success, Is.True);
            Assert.That(context.Registry, Is.SameAs(registry));
            Assert.That(context.Package, Is.SameAs(package));
            Assert.That(context.Version, Is.SameAs(version));
        }
    }
}
