using NUnit.Framework;
using Party.Shared.Models;
using Party.Shared.Models.Registries;

namespace Party.Shared.Registries
{
    public class PackageFullNameTests
    {

        [TestCase(null)]
        [TestCase("")]
        [TestCase("something/my-package")]
        [TestCase("scripts/my-package/what")]
        [TestCase("scripts/!package")]
        public void InvalidValues(string input)
        {
            var success = PackageFullName.TryParsePackage(input, out var result);

            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        }

        [TestCase("scripts/my-package", RegistryPackageType.Scripts, "my-package", null)]
        [TestCase("scripts/my-package@1.0.0", RegistryPackageType.Scripts, "my-package", "1.0.0")]
        public void ValidValues(string input, RegistryPackageType type, string name, string version)
        {
            var success = PackageFullName.TryParsePackage(input, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Type, Is.EqualTo(type));
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Version, Is.EqualTo(version));
        }
    }
}
