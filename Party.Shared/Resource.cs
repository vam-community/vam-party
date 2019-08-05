using System.IO;

namespace Party.Shared
{
    public abstract class Resource
    {
        private string _sceneFile;

        public string Filename => Path.GetFileName(_sceneFile);

        public Resource(string sceneFile)
        {
            _sceneFile = sceneFile;
        }
    }
}