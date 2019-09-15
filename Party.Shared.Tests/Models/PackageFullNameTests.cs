using NUnit.Framework;
using Party.Shared.Models;

namespace Party.Shared
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

        [TestCase("scripts/my-package", PackageTypes.Scripts, "my-package", null)]
        [TestCase("scripts/my-package@1.0.0", PackageTypes.Scripts, "my-package", "1.0.0")]
        public void ValidValues(string input, PackageTypes type, string name, string version)
        {
            var success = PackageFullName.TryParsePackage(input, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Type, Is.EqualTo(type));
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Version, Is.EqualTo(version));
        }
    }
}
