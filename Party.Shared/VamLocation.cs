using System;
using System.IO;

namespace Party.Shared
{
    public class VamLocation
    {
        public static VamLocation RelativeTo(VamLocation parent, string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(parent.ContainingDirectory, relativePath));
            return VamLocation.Absolute(parent.SavesDirectory, fullPath);
        }

        public static VamLocation Absolute(string savesDirectory, string path)
        {
            if (!path.StartsWith(savesDirectory))
                throw new InvalidOperationException("Path must live in the Saves directory");

            return new VamLocation(savesDirectory, path.Substring(savesDirectory.Length).TrimStart(Path.DirectorySeparatorChar));
        }

        public string SavesDirectory { get; }
        public string RelativePath { get; }
        public string FullPath => Path.Combine(SavesDirectory, RelativePath);
        public string ContainingDirectory => Path.Combine(SavesDirectory, Path.GetDirectoryName(RelativePath));

        public VamLocation(string savesDirectory, string relativePath)
        {
            SavesDirectory = savesDirectory;
            RelativePath = relativePath;
        }
    }
}
