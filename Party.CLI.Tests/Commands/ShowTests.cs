using Moq;
using NUnit.Framework;
using Party.Shared.Results;
using System.Linq;
using System.Threading.Tasks;

namespace Party.CLI
{
    public class ShowTests : CommandTestsBase
    {
        [Test]
        public async Task Show()
        {
            _controller.Setup(x => x.GetRegistryAsync()).ReturnsAsync(new Registry
            {
                Scripts = new[]
                {
                    new RegistryScript
                    {
                        Name = "cool-thing",
                        Author = new RegistryScriptAuthor
                        {
                            Name = "some dude"
                        },
                        Versions = new []
                        {
                            new RegistryScriptVersion
                            {
                                Version = "1.2.3",
                                Files = new []
                                {
                                    new RegistryFile
                                    {
                                        Filename = "File 1.cs"
                                    }
                                }.ToList()
                            }
                        }.ToList()
                    }
                }.ToList()
            });
            _controller.Setup(x => x.GetSavesAsync()).ReturnsAsync(new SavesMapResult());

            var result = await _program.Execute(new[] { "show", "cool-thing" });

            Assert.That(GetOutput(), Is.EqualTo(new[]{
                "Package cool-thing, by some dude",
                "Files:",
                "- File 1.cs"
            }));
            Assert.That(result, Is.EqualTo(0));
        }
    }
}
