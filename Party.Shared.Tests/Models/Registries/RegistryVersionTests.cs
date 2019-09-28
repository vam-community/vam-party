using NUnit.Framework;
using Party.Shared.Models.Registries;

namespace Party.Shared.Registries
{
    public class RegistryVersionTests
    {
        [TestCase("1.0.0")]
        [TestCase("0.0.1")]
        [TestCase("10.30.1945")]
        [TestCase("2.0.4-preview5")]
        public void CanValidateVersionNames(string version)
        {
            Assert.That(RegistryPackageVersion.ValidVersionNameRegex.IsMatch(version), Is.True);
        }
    }
}
