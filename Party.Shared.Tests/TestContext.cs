using System;
using System.IO;
using System.Linq;

namespace Party.Shared.Tests
{
    public static class TestContext
    {
        public static string GetTestsSavesDirectory()
        {
            string repoPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..");
            string savesPath = Path.Combine(repoPath, "TestData", "Saves");
            return Path.GetFullPath(savesPath);
        }

        public static VamLocation GetSavesFile(params string[] parts)
        {
            var savesPath = GetTestsSavesDirectory();
            return VamLocation.Absolute(savesPath, Path.Combine(new[] { GetTestsSavesDirectory() }.Concat(parts).ToArray()));
        }
    }
}
