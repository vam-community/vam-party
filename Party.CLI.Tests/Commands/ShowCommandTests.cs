using Moq;
using NUnit.Framework;
using Party.Shared.Models.Registries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Party.CLI
{
    public class ShowCommandTests : CommandTestsBase
    {
        [Test]
        [SetUICulture("en-US")]
        public async Task ShowWithMinimalInfo()
        {
            _controller.Setup(x => x.HealthCheck());
            DateTimeOffset created = new DateTimeOffset(2010, 11, 12, 0, 0, 0, 0, TimeSpan.Zero);
            _controller.Setup(x => x.GetRegistryAsync()).ReturnsAsync(new Registry
            {
                Packages = new SortedSet<RegistryPackage>(new[]
                {
                    new RegistryPackage
                    {
                        Type = RegistryPackageType.Scripts,
                        Name = "cool-thing",
                        Author = "some dude",
                        Versions = new SortedSet<RegistryPackageVersion>(new []
                        {
                            new RegistryPackageVersion
                            {
                                Version = "1.2.3",
                                Created = created,
                                Files = new SortedSet<RegistryFile>
                                {
                                    new RegistryFile
                                    {
                                        Filename = "File 1.cs"
                                    }
                                }
                            }
                        })
                    }
                })
            });

            var result = await _program.Execute(new[] { "show", "scripts/cool-thing" });

            Assert.That(GetOutput(), Is.EqualTo(new[]{
                "Party, a Virt-A-Mate package manager, is still in it's early stages. Please file any issue or ideas at https://github.com/vam-community/vam-party/issues",
                "Package scripts/cool-thing",
                $"Last version v1.2.3, published {created.ToLocalTime().ToString("D")}",
                "Versions:",
                $"- v1.2.3, published {created.ToLocalTime().ToString("D")}: (no release notes)",
                "Author: some dude",
                "Files in v1.2.3:",
                "- File 1.cs: not available in registry"
            }));
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        [SetUICulture("en-US")]
        public async Task ShowWithExtraInfo()
        {
            _controller.Setup(x => x.HealthCheck());
            DateTimeOffset created = new DateTimeOffset(2010, 11, 12, 0, 0, 0, 0, TimeSpan.Zero);
            _controller.Setup(x => x.GetRegistryAsync()).ReturnsAsync(new Registry
            {
                Authors = new SortedSet<RegistryAuthor>(new[]
                {
                    new RegistryAuthor{
                        Name = "some dude",
                        Github = "https://github.com/...profile",
                        Reddit = "https://reddit.com/...profile"
                    }
                }),
                Packages = new SortedSet<RegistryPackage>(new[]
                {
                    new RegistryPackage
                    {
                        Type = RegistryPackageType.Scripts,
                        Name = "cool-thing",
                        Author = "some dude",
                        Tags = new List<string>(new[]{"tag1", "tag2"}),
                        Description = "This does some things",
                        Homepage = "https://reddit.com/...homepage",
                        Repository = "https://github.com/...repo",
                        Versions = new SortedSet<RegistryPackageVersion>(new []
                        {
                            new RegistryPackageVersion
                            {
                                Version = "1.2.3",
                                Created = created,
                                Notes = "Some cool new stuff",
                                Files = new SortedSet<RegistryFile>
                                {
                                    new RegistryFile
                                    {
                                        Filename = "File 1.cs",
                                        Url = "https://example.org/File%201.cs"
                                    }
                                }
                            }
                        })
                    }
                })
            });

            var result = await _program.Execute(new[] { "show", "scripts/cool-thing" });

            Assert.That(GetOutput(), Is.EqualTo(new[]{
                "Party, a Virt-A-Mate package manager, is still in it's early stages. Please file any issue or ideas at https://github.com/vam-community/vam-party/issues",
                "Package scripts/cool-thing",
                $"Last version v1.2.3, published {created.ToLocalTime().ToString("D")}",
                "Versions:",
                $"- v1.2.3, published {created.ToLocalTime().ToString("D")}: Some cool new stuff",
                "Description: This does some things",
                "Tags: tag1, tag2",
                "Repository: https://github.com/...repo",
                "Homepage: https://reddit.com/...homepage",
                "Author: some dude",
                "- Github: https://github.com/...profile",
                "- Reddit: https://reddit.com/...profile",
                "Files in v1.2.3:",
                "- File 1.cs: https://example.org/File%201.cs"
            }));
            Assert.That(result, Is.EqualTo(0));
        }
    }
}
