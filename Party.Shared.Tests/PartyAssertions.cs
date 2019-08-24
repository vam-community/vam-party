using Newtonsoft.Json;
using NUnit.Framework;

namespace Party.Shared
{
    public static class PartyAssertions
    {
        public static void AreDeepEqual(object expected, object actual)
        {
            var expectedJson = JsonConvert.SerializeObject(expected);
            var actualJson = JsonConvert.SerializeObject(actual);
            Assert.AreEqual(expectedJson, actualJson);
        }
    }
}
