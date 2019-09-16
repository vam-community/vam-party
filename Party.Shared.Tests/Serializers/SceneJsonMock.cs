using System.Collections.Generic;
using System.Linq;
using Party.Shared.Serializers;

namespace Party.Shared
{

    internal class SceneJsonMock : ISceneJson
    {
        public ICollection<IAtomJson> Atoms { get; }

        public SceneJsonMock(params AtomJsonMock[] atoms)
        {
            Atoms = atoms.ToList<IAtomJson>();
        }
    }

    internal class AtomJsonMock : IAtomJson
    {
        public ICollection<IPluginJson> Plugins { get; }

        public AtomJsonMock(params PluginJsonMock[] plugins)
        {
            Plugins = plugins.ToList<IPluginJson>();
        }
    }

    internal class PluginJsonMock : IPluginJson
    {
        public string Path { get; set; }

        public PluginJsonMock(string path)
        {
            Path = path;
        }
    }
}
