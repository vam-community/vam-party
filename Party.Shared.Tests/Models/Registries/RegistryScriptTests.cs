using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Party.Shared.Models.Registries;

namespace Party.Shared.Registries
{
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
}
