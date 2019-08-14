using System.IO;

namespace Party.Shared.Resources
{
    public abstract class Resource
    {
        public string FullPath { get; }
        public string Hash { get; }
        public string Name { get => Path.GetFileName(FullPath); }

        protected Resource(string fullPath, string hash = null)
        {
            FullPath = fullPath;
            Hash = hash;
        }

        public string GetIdentifier()
        {
            // TODO: Instead, pre-calculate the hash
            return Hash != null ? $"{System.IO.Path.GetFileName(FullPath)}{Hash}" : FullPath;
        }
    }
}
