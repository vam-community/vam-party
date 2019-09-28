using NUnit.Framework;
using Party.Shared.Models.Registries;

namespace Party.Shared.Registries
{
    public class RegistryVersionStringTests
    {
        [TestCase("1.0.0", 1, 0, 0, "")]
        [TestCase("0.0.1234", 0, 0, 1234, "")]
        [TestCase("2.10.0-preview5", 2, 10, 0, "preview5")]
        public void CanCastFromToString(string version, int major, int minor, int revision, string extra)
        {
            var versionStruct = new RegistryVersionString(version);

            Assert.That(versionStruct.Major, Is.EqualTo(major));
            Assert.That(versionStruct.Minor, Is.EqualTo(minor));
            Assert.That(versionStruct.Revision, Is.EqualTo(revision));
            Assert.That(versionStruct.Extra, Is.EqualTo(extra));

            Assert.That(versionStruct.ToString(), Is.EqualTo(version));
        }

        [TestCase("0.0.1", "0.0.2", false)]
        [TestCase("10.0.5", "10.0.5", true)]
        [TestCase("1.0.0", "1.0.0-preview", false)]
        [TestCase("1.0.0-preview", "1.0.0-preview", true)]
        [TestCase("1.0.0-preview", "1.0.0-preview2", false)]
        public void CanCompareVersions(string first, string second, bool expected)
        {
            Assert.That(first.Equals(second), Is.EqualTo(expected));
        }
    }
}
