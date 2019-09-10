using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Party.Shared.Models;

namespace Party.Shared
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
    }

    public class RegistryScriptTests
    {
        [Test]
        public void CanGetLatestVersion()
        {
            var script = new RegistryPackage
            {
                Versions = new SortedSet<RegistryPackageVersion>(new[]
                {
                    new RegistryPackageVersion{ Version = "1.0.0" },
                    new RegistryPackageVersion{ Version = "2.0.5" },
                    new RegistryPackageVersion{ Version = "2.0.4" },
                })
            };

            Assert.That(script.GetLatestVersion().Version.ToString(), Is.EqualTo("2.0.5"));
        }

        [Test]
        public void CanSort()
        {
            var script = new RegistryPackage
            {
                Versions = new SortedSet<RegistryPackageVersion>(new[]
                {
                    new RegistryPackageVersion{ Version = "1.0.0" },
                    new RegistryPackageVersion{ Version = "2.0.5" },
                    new RegistryPackageVersion{ Version = "2.0.4" },
                })
            };

            Assert.That(script.Versions.Select(v => v.Version.ToString()).ToArray(), Is.EqualTo(new[] { "2.0.5", "2.0.4", "1.0.0" }));
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
            Assert.That(RegistryPackageVersion.ValidVersionNameRegex.IsMatch(version), Is.True);
        }
    }

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
