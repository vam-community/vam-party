using System.IO;

namespace Party.Shared.Tests
{
    public static class StringExtensions
    {
        public static string OnWindows(this string path)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                return path.Replace('/', '\\');
            }
            else
            {
                return path;
            }
        }
    }
}
