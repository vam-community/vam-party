using System;
using System.IO;

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
    }
}
