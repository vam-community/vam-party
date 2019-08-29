using NUnit.Framework;
using Party.Shared.Models;

namespace Party.Shared
{
    public class RegistryTests
    {
        [TestCase("package")]
        [TestCase("my-package")]
        [TestCase("package25")]
        public void CanValidateNames(string version)
        {
            Assert.That(RegistryScript.ValidNameRegex.IsMatch(version), Is.True);
        }
    }

    public class RegistryVersionTests
    {
        [TestCase("1.0.0")]
        [TestCase("0.0.1")]
        [TestCase("10.30.1945")]
        [TestCase("2.0.4-preview5")]
        public void CanValidateVersionNames(string version)
        {
            Assert.That(RegistryScriptVersion.ValidVersionNameRegex.IsMatch(version), Is.True);
        }
    }
}
