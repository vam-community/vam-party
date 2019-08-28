using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Serializers;

namespace Party.Shared
{
    public class ScriptListSerializerTests
    {
        [Test]
        public async Task CanGetScripts()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                     @"C:\VaM\Saves\Scene 1.json",
                     new MockFileData(string.Join("\n", new[]
                        {
                            "Script 1.cs",
                            "Script 2.cs",
                            ""
                        }
                ))},
            });

            var result = await new ScriptListSerializer().GetScriptsAsync(fileSystem, @"C:\VaM\Saves\ADD_ME.cslit"));

            Assert.That(result, Is.EqualTo(new[] { "Script 1.cs", "Script 2.cs" }));
        }
    }
}
