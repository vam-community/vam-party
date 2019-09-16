using System.IO;

namespace Party.Shared.Models.Local
{
    public abstract class LocalFile
    {
        public string FullPath { get; }
        public string Hash { get; }
        public string FileName { get => Path.GetFileName(FullPath); }

        protected LocalFile(string fullPath, string hash)
        {
            FullPath = fullPath;
            Hash = hash;
        }
    }
}
