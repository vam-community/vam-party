using System.IO;

namespace Party.Shared
{
    public abstract class Resource
    {
        public string FullPath { get; }

        public string Filename => Path.GetFileName(FullPath);

        public string ContainingDirectory => Path.GetDirectoryName(FullPath);

        public Resource(string path)
        {
            FullPath = path;
        }
    }
}
