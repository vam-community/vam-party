using System.Collections.Generic;
using System.IO;

namespace Party.Shared
{
    public class VamSaves
    {
        private string _currentDirectory;

        public VamSaves(string currentDirectory)
        {
            _currentDirectory = currentDirectory;
        }

        public IEnumerable<Scene> GetAllScenes()
        {
            foreach (var sceneFile in Directory.EnumerateFiles(_currentDirectory, "*.json", SearchOption.AllDirectories))
            {
                yield return new Scene(sceneFile);
            }
        }
    }
}
