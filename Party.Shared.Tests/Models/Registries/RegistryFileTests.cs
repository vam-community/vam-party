using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Party.Shared.Models.Registries;

namespace Party.Shared
{
    public class RegistryFileTests
    {
        [Test]
        public void SortsCorrectly()
        {
            var files = new SortedSet<RegistryFile>(new[]
            {
                new RegistryFile { Filename = "/root/file" },
                new RegistryFile { Filename = "file1" },
                new RegistryFile { Filename = "file3", Hash = new RegistryHash { Type = "type", Value = "123" } },
                new RegistryFile { Filename = "file2" },
                new RegistryFile { Filename = "folder2/file1" },
                new RegistryFile { Filename = "folder2/file2" },
                new RegistryFile { Filename = "folder1/file1" },
                new RegistryFile { Filename = "folder1/file2" },
                new RegistryFile { Filename = null },
            });

            Assert.That(files.Select(f => f.Filename), Is.EqualTo(new[]
            {
                "file3",
                "file1",
                "file2",
                "folder1/file1",
                "folder1/file2",
                "folder2/file1",
                "folder2/file2",
                "/root/file",
                null
            }));
        }
    }
}
