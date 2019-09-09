using System.Collections.Generic;
using System.Linq;
using Party.Shared.Models;

namespace Party.Shared
{
    internal static class ResultFactory
    {
        #region Registry
        internal static Registry Reg(params RegistryScript[] scripts)
        {
            return new Registry
            {
                Scripts = new SortedSet<RegistryScript>(scripts)
            };
        }

        internal static RegistryScript RegScript(string name, params RegistryScriptVersion[] versions)
        {
            return new RegistryScript
            {
                Name = name,
                Versions = new SortedSet<RegistryScriptVersion>(versions)
            };
        }

        internal static RegistryScriptVersion RegVer(string version, params RegistryFile[] files)
        {
            return new RegistryScriptVersion
            {
                Version = version,
                Files = new SortedSet<RegistryFile>(files)
            };
        }

        internal static RegistryFile RegFile(string filename, string hash, string url = null)
        {
            return new RegistryFile
            {
                Filename = filename,
                Hash = new RegistryFileHash
                {
                    Value = hash
                },
                Url = url
            };
        }
        #endregion

        #region Saves Map
        internal static SavesMapBuilder SavesMap()
        {
            return new SavesMapBuilder();
        }

        internal class SavesMapBuilder
        {
            private readonly List<Script> _scripts = new List<Script>();
            private readonly List<Scene> _scenes = new List<Scene>();
            private readonly List<SavesError> _errors = new List<SavesError>();

            internal SavesMapBuilder WithScript(Script script, out Script outScript)
            {
                _scripts.Add(script);
                outScript = script;
                return this;
            }

            internal SavesMapBuilder Referencing(Scene scene, out Scene outScene)
            {
                _scripts.Last().Scenes.Add(scene);
                _scenes.Add(scene);
                outScene = scene;
                return this;
            }

            internal SavesMapBuilder WithScene(Scene scene)
            {
                _scenes.Add(scene);
                return this;
            }

            internal SavesMap Build()
            {
                return new SavesMap
                {
                    Scripts = _scripts.ToArray(),
                    Scenes = _scenes.ToArray(),
                    Errors = _errors.ToArray()
                };
            }
        }
        #endregion
    }
}
