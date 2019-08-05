using System.IO;

namespace Party.Shared
{
    public class Scene
    {
        private string _sceneFile;

        public string Filename => Path.GetFileName(_sceneFile);

        public Scene(string sceneFile)
        {
            _sceneFile = sceneFile;
        }
    }
}
