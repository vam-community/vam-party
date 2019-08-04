using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class VamSavesTests
    {
        [Test]
        public void SomeTest()
        {
            var saves =  new VamSaves();
            Assert.That(saves, Is.Not.Null);
        }
    }
}
