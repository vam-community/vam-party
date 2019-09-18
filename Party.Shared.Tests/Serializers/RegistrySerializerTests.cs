using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        public void CanSerializeInOrder()
        {
            var result = _serializer.Serialize(new Registry
            {
                Authors = new SortedSet<RegistryAuthor>(new[]
                {
                    new RegistryAuthor
                    {
                        Name = "User 2"
                    },
                    new RegistryAuthor
                    {
                        Name = "User 1"
                    }
                }),
                Packages = new SortedSet<RegistryPackage>(new[]
                {
                    new RegistryPackage
                    {
                        Type = RegistryPackageType.Scripts,
                        Name = "package-2",
                        Tags = new List<string> { "tag1", "tag2" },
                        Versions = new SortedSet<RegistryPackageVersion>(new[]
                        {
                            new RegistryPackageVersion
                            {
                                Version = "1.0.0",
                                Files = new SortedSet<RegistryFile>(new[]
                                {
                                    new RegistryFile
                                    {
                                        Filename = "file2.cs"
                                    },
                                    new RegistryFile
                                    {
                                        Filename = "file1.cs"
                                    }
                                })
                            },
                            new RegistryPackageVersion
                            {
                                Version = "1.2.0",
                                Files = new SortedSet<RegistryFile>(new[]
                                {
                                    new RegistryFile
                                    {
                                        Filename = "file2.cs"
                                    },
                                    new RegistryFile
                                    {
                                        Filename = "file1.cs"
                                    }
                                })
                            }
                        })
                    },
                    new RegistryPackage
                    {
                        Type = RegistryPackageType.Scripts,
                        Name = "package-1",
                        Tags = new List<string> { "tag1", "tag2" },
                        Versions = new SortedSet<RegistryPackageVersion>(new[]
                        {
                            new RegistryPackageVersion
                            {
                                Version = "1.0.0",
                                Files = new SortedSet<RegistryFile>(new[]
                                {
                                    new RegistryFile
                                    {
                                        Filename = "file2.cs"
                                    },
                                    new RegistryFile
                                    {
                                        Filename = "file1.cs"
                                    }
                                })
                            },
                            new RegistryPackageVersion
                            {
                                Version = "1.2.0",
                                Files = new SortedSet<RegistryFile>(new[]
                                {
                                    new RegistryFile
                                    {
                                        Filename = "file2.cs"
                                    },
                                    new RegistryFile
                                    {
                                        Filename = "file1.cs"
                                    }
                                })
                            }
                        })
                    },
                })
            });

            Assert.That(result, Is.EqualTo(@"{
  ""authors"": [
    {
      ""name"": ""User 1""
    },
    {
      ""name"": ""User 2""
    }
  ],
  ""packages"": [
    {
      ""type"": ""scripts"",
      ""name"": ""package-1"",
      ""tags"": [""tag1"", ""tag2""],
      ""versions"": [
        {
          ""version"": ""1.2.0"",
          ""files"": [
            {
              ""filename"": ""file1.cs""
            },
            {
              ""filename"": ""file2.cs""
            }
          ]
        },
        {
          ""version"": ""1.0.0"",
          ""files"": [
            {
              ""filename"": ""file1.cs""
            },
            {
              ""filename"": ""file2.cs""
            }
          ]
        }
      ]
    },
    {
      ""type"": ""scripts"",
      ""name"": ""package-2"",
      ""tags"": [""tag1"", ""tag2""],
      ""versions"": [
        {
          ""version"": ""1.2.0"",
          ""files"": [
            {
              ""filename"": ""file1.cs""
            },
            {
              ""filename"": ""file2.cs""
            }
          ]
        },
        {
          ""version"": ""1.0.0"",
          ""files"": [
            {
              ""filename"": ""file1.cs""
            },
            {
              ""filename"": ""file2.cs""
            }
          ]
        }
      ]
    }
  ]
}
"), result);
        }

        [Test]
        public void CanSerializeAndDeserialize()
        {
            var registry = new Registry
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
                        Type = RegistryPackageType.Scripts,
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
            };

            var serialized = _serializer.Serialize(registry);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
            var deserialized = _serializer.Deserialize(stream);

            PartyAssertions.AreDeepEqual(registry, deserialized);
        }
    }
}
