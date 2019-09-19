using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Serializers;
using Party.Shared.Utils;

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
                     @"C:\VaM\Saves\ADD_ME.cslist",
                     new MockFileData(string.Join("\n", new[]
                        {
                            "Script 1.cs",
                            "Script 2.cs",
                            ""
                        }
                ))},
            });

            var result = await new ScriptListSerializer(fileSystem, new Throttler()).GetScriptsAsync(@"C:\VaM\Saves\ADD_ME.cslist");

            Assert.That(result, Is.EqualTo(new[] { "Script 1.cs", "Script 2.cs" }));
        }
    }
}
