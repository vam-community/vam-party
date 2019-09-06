using System.IO;

namespace Party.Shared.Resources
{
    public abstract class Resource
    {
        public string FullPath { get; }
        public string Hash { get; }
        public string FileName { get => Path.GetFileName(FullPath); }

        protected Resource(string fullPath, string hash)
        {
            FullPath = fullPath;
            Hash = hash;
        }
    }
}
