using Newtonsoft.Json;
using NUnit.Framework;

namespace Party.Shared
{
    internal static class PartyAssertions
    {
        internal static void AreDeepEqual(object expected, object actual)
        {
            var expectedJson = JsonConvert.SerializeObject(expected);
            var actualJson = JsonConvert.SerializeObject(actual);
            Assert.AreEqual(expectedJson, actualJson, $"Actual\n{JsonConvert.SerializeObject(actual, Formatting.Indented)}\nExpected:\n{JsonConvert.SerializeObject(expected, Formatting.Indented)}");
        }
    }
}
