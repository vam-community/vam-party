using System.Collections.Generic;
using NUnit.Framework;
using Party.Shared.Models.Registries;

namespace Party.Shared.Serializers
{
    public class RegistrySerializerTests
    {
        private RegistrySerializer _serializer;

        [SetUp]
        public void BeforeEach()
        {
            _serializer = new RegistrySerializer();
        }

        [Test]
        public void CanSerialize()
        {
            var result = _serializer.Serialize(new Registry
            {
                Authors = new SortedSet<RegistryAuthor>(new[]
                {
                    new RegistryAuthor
                    {
                        Name = "John Doe"
                    }
                }),
                Packages = new SortedSet<RegistryPackage>(new[]
                {
                    new RegistryPackage
                    {
                        Name = "some-package",
                        Tags = new List<string> { "tag1", "tag2" },
                        Versions = new SortedSet<RegistryPackageVersion>(new[]
                        {
                            new RegistryPackageVersion
                            {
                                Version = "1.0.0"
                            }
                        })
                    }
                })
            });

            Assert.That(result, Is.EqualTo(@"{
  ""authors"": [
    {
      ""name"": ""John Doe""
    }
  ],
  ""packages"": [
    {
      ""name"": ""some-package"",
      ""tags"": [""tag1"", ""tag2""],
      ""versions"": [
        {
          ""version"": ""1.0.0"",
          ""files"": []
        }
      ]
    }
  ]
}
"), result);
        }
    }
}
