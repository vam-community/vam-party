using Moq;
using NUnit.Framework;
using Party.Shared.Models;
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
        public async Task ShowWithExtraInfo()
        {
            DateTimeOffset created = new DateTimeOffset(2010, 11, 12, 0, 0, 0, 0, TimeSpan.Zero);
            _controller
                .Setup(x => x.HealthCheck());
            _controller
                .Setup(x => x.GetDisplayPath(It.IsAny<string>()))
                .Returns((string path) => path.Replace("ROOT/", ""));
            _controller
                .Setup(x => x.GetRegistryAsync())
                .ReturnsAsync(new Registry
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
            _controller
                .Setup(x => x.GetInstalledPackageInfoAsync(It.IsAny<RegistryPackageVersionContext>()))
                .ReturnsAsync(new LocalPackageInfo
                {
                    Files = new[]
                    {
                        new InstalledFileInfo
                        {
                            FullPath = "ROOT/Folder/File.cs",
                            Status = FileStatus.Installed
                        }
                    }
                });

            var result = await _program.Execute(new[] { "show", "scripts/cool-thing" });

            Assert.That(GetOutput(), Is.EqualTo(new[]
            {
                "[color:green]Party, a Virt-A-Mate package manager, is still in it's early stages. Please file any issue or ideas at https://github.com/vam-community/vam-party/issues[/color]",
                "[color:cyan]cool-thing v1.2.3 (scripts)[/color]",
                "===========================",
                "[color:blue]Info:[/color]",
                "  Description: This does some things",
                "  Tags: tag1, tag2",
                "  Repository: https://github.com/...repo",
                "  Homepage: https://reddit.com/...homepage",
                "[color:blue]Author:[/color] some dude",
                "  Github: https://github.com/...profile",
                "  Reddit: https://reddit.com/...profile",
                "[color:blue]Versions:[/color]",
                $"  v1.2.3, {created.ToLocalTime().ToString("d")}: Some cool new stuff",
                "[color:blue]Files (for v1.2.3):[/color]",
                "- Folder/File.cs [color:green][installed][/color]"
            }));
            Assert.That(result, Is.EqualTo(0));
        }
    }
}
