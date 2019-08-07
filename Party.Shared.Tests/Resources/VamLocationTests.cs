using NUnit.Framework;
using Party.Shared.Resources;

namespace Party.Shared.Tests.Resources
{
    public class VamLocationTests
    {
        private const string SavesDirectory = @"C:\VaM\Saves";

        [Test]
        public void CanGetRepresentations()
        {
            var loc = new VamLocation(SavesDirectory, @"Folder\File.json");

            Assert.That(loc.SavesDirectory, Is.EqualTo(SavesDirectory));
            Assert.That(loc.RelativePath, Is.EqualTo(@"Folder\File.json"));
            Assert.That(loc.FullPath, Is.EqualTo(@"C:\VaM\Saves\Folder\File.json"));
            Assert.That(loc.ContainingDirectory, Is.EqualTo(@"C:\VaM\Saves\Folder"));
        }

        [Test]
        public void CanCreateAbsolute()
        {
            var loc = VamLocation.Absolute(SavesDirectory, @"C:\VaM\Saves\Folder\File.json");

            Assert.That(loc.SavesDirectory, Is.EqualTo(SavesDirectory));
            Assert.That(loc.RelativePath, Is.EqualTo(@"Folder\File.json"));
        }

        [Test]
        public void CanCreateRelativeTo()
        {
            var loc = VamLocation.RelativeTo(new VamLocation(SavesDirectory, @"Folder\File.json"), @"..\Other Folder\Other File.cs");

            Assert.That(loc.SavesDirectory, Is.EqualTo(SavesDirectory));
            Assert.That(loc.RelativePath, Is.EqualTo(@"Other Folder\Other File.cs"));
        }
    }
}
