using Moq;
using NUnit.Framework;
using Party.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Party.CLI
{
    public class ShowTests : CommandTestsBase
    {
        [Test]
        [SetUICulture("en-US")]
        public async Task ShowWithMinimalInfo()
        {
            _controller.Setup(x => x.HealthCheck());
            DateTimeOffset created = new DateTimeOffset(2010, 11, 12, 0, 0, 0, 0, TimeSpan.Zero);
            _controller.Setup(x => x.GetRegistryAsync()).ReturnsAsync(new Registry
            {
                Scripts = new SortedSet<RegistryScript>(new[]
                {
                    new RegistryScript
                    {
                        Name = "cool-thing",
                        Author = "some dude",
                        Versions = new SortedSet<RegistryScriptVersion>(new []
                        {
                            new RegistryScriptVersion
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
            _controller.Setup(x => x.GetSavesAsync(null)).ReturnsAsync(new SavesMap());

            var result = await _program.Execute(new[] { "show", "cool-thing" });

            Assert.That(GetOutput(), Is.EqualTo(new[]{
                "Package cool-thing",
                $"Last version v1.2.3, published {created.ToLocalTime().ToString("D")}",
                "Versions:",
                $"- v1.2.3, published {created.ToLocalTime().ToString("D")}: (no release notes)",
                "Author: some dude",
                "Files:",
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
                Scripts = new SortedSet<RegistryScript>(new[]
                {
                    new RegistryScript
                    {
                        Name = "cool-thing",
                        Author = "some dude",
                        Tags = new List<string>(new[]{"tag1", "tag2"}),
                        Description = "This does some things",
                        Homepage = "https://reddit.com/...homepage",
                        Repository = "https://github.com/...repo",
                        Versions = new SortedSet<RegistryScriptVersion>(new []
                        {
                            new RegistryScriptVersion
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
            _controller.Setup(x => x.GetSavesAsync(null)).ReturnsAsync(new SavesMap());

            var result = await _program.Execute(new[] { "show", "cool-thing" });

            Assert.That(GetOutput(), Is.EqualTo(new[]{
                "Package cool-thing",
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
                "Files:",
                "- File 1.cs: https://example.org/File%201.cs"
            }));
            Assert.That(result, Is.EqualTo(0));
        }
    }
}
