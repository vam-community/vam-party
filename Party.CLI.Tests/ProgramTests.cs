using NUnit.Framework;
using System.Threading.Tasks;

namespace Party.CLI
{
    public class ProgramTests
    {
        [Test]
        public async Task CanInitializeAllDependencies()
        {
            var result = await Program.Main(new[] { "--version" });

            Assert.That(result, Is.EqualTo(0));
        }
    }
}
