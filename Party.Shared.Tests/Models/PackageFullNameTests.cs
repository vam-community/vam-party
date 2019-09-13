using NUnit.Framework;
using Party.Shared.Models;

namespace Party.Shared
{
    public class PackageFullNameTests
    {
        [Test]
        public void CanParse()
        {
            var success = PackageFullName.TryParsePackage("scripts/my-package", out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Type, Is.EqualTo(PackageTypes.Scripts));
            Assert.That(result.Name, Is.EqualTo("my-package"));
        }
    }
}
