using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Party.Shared.Models;
using Party.Shared.Models.Registries;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class GetInstalledPackageInfoHandlerTests
    {
        [Test]
        public async Task Installed()
        {
            GivenContext(
                @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0\Script 1.cs",
                "class Script1 {}",
                new[] { TestFactory.RegFile("Script 1.cs", "64816EFE9CCFED7730C8FCF412178C23E5FE94304B7317ED03FE1D005C490C66") },
                out var handler, out var context);

            var info = await handler.GetInstalledPackageInfoAsync(context);

            PartyAssertions.AreDeepEqual(new LocalPackageInfo
            {
                Corrupted = false,
                Installable = true,
                Installed = true,
                PackageFolder = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0",
                Files = new[]{
                    new InstalledFileInfo{
                        FullPath = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0\Script 1.cs",
                        RegistryFile = context.Version.Files.First(),
                        Status = FileStatus.Installed
                    }
                }
            }, info);
        }

        [Test]
        public async Task NotInstalled()
        {
            GivenContext(
                @"C:\VaM\Custom\Scripts\elsewhere\Script 1.cs",
                "class Script1 {}",
                new[]
                {
                    new RegistryFile
                    {
                        Filename = "Script 1.cs",
                        Hash = new RegistryHash { Type = Hashing.Type, Value = "64816EFE9CCFED7730C8FCF412178C23E5FE94304B7317ED03FE1D005C490C66" },
                        Url = "https://example.org/download/Script1%20.cs"
                    }
                },
                out var handler, out var context);

            var info = await handler.GetInstalledPackageInfoAsync(context);

            PartyAssertions.AreDeepEqual(new LocalPackageInfo
            {
                Corrupted = false,
                Installable = true,
                Installed = false,
                PackageFolder = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0",
                Files = new[]{
                    new InstalledFileInfo{
                        FullPath = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0\Script 1.cs",
                        RegistryFile = context.Version.Files.First(),
                        Status = FileStatus.NotInstalled
                    }
                }
            }, info);
        }

        [Test]
        public async Task NotDownloadable()
        {
            GivenContext(
                @"C:\VaM\Custom\Scripts\elsewhere\Script 1.cs",
                "class Script1 {}",
                new[]
                {
                    new RegistryFile
                    {
                        Filename = "Script 1.cs",
                        Hash = new RegistryHash { Type = Hashing.Type, Value = "64816EFE9CCFED7730C8FCF412178C23E5FE94304B7317ED03FE1D005C490C66" },
                        Url = null
                    }
                },
                out var handler, out var context);

            var info = await handler.GetInstalledPackageInfoAsync(context);

            PartyAssertions.AreDeepEqual(new LocalPackageInfo
            {
                Corrupted = false,
                Installable = false,
                Installed = false,
                PackageFolder = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0",
                Files = new[]{
                    new InstalledFileInfo{
                        FullPath = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0\Script 1.cs",
                        RegistryFile = context.Version.Files.First(),
                        Status = FileStatus.NotDownloadable,
                        Reason = "No URL provided."
                    }
                }
            }, info);
        }

        [Test]
        public async Task HashMismatch()
        {
            GivenContext(
                @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0\Script 1.cs",
                "class Script1 {}",
                new[] { TestFactory.RegFile("Script 1.cs", "0000000000000000000000000000000000000000000000000000000000000000") },
                out var handler, out var context);

            var info = await handler.GetInstalledPackageInfoAsync(context);

            PartyAssertions.AreDeepEqual(new LocalPackageInfo
            {
                Corrupted = true,
                Installable = false,
                Installed = false,
                PackageFolder = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0",
                Files = new[]{
                    new InstalledFileInfo{
                        FullPath = @"C:\VaM\Custom\Scripts\some-author\my-script\1.0.0\Script 1.cs",
                        RegistryFile = context.Version.Files.First(),
                        Status = FileStatus.HashMismatch,
                        Reason = "Expected hash 0000000000000000000000000000000000000000000000000000000000000000, file on disk was 64816EFE9CCFED7730C8FCF412178C23E5FE94304B7317ED03FE1D005C490C66."
                    }
                }
            }, info);
        }

        private static void GivenContext(string path, string content, RegistryFile[] files, out GetInstalledPackageInfoHandler handler, out RegistryPackageVersionContext context)
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { path, new MockFileData(content) },
            });

            var folders = new FoldersHelper(fileSystem, @"C:\VaM", new[] { "Saves", "Custom" }, true);

            handler = new GetInstalledPackageInfoHandler(fileSystem, folders);

            var registry = TestFactory.Reg(
                TestFactory.RegScript("my-script", "some-author",
                    TestFactory.RegVer("1.0.0", files)));

            context = registry.TryGetPackageVersion(RegistryPackageType.Scripts, "my-script", "1.0.0", out var x) ? x : throw new Exception("Could not get package context");
        }
    }
}
