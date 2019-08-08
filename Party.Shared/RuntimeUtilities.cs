using System;
using System.IO;
using System.Reflection;

namespace Party.Shared
{
    public static class RuntimeUtilities
    {
        public static string GetApplicationRoot()
        {
            return new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
        }
    }
}
