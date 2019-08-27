using System.Collections.Generic;
using System.Linq;
using Party.Shared.Resources;
using Party.Shared.Results;

namespace Party.Shared
{
    internal static class ResultFactory
    {
        #region Registry
        internal static RegistryResult Reg(params RegistryScript[] scripts)
        {
            return new RegistryResult
            {
                Scripts = scripts.ToList()
            };
        }

        internal static RegistryScript RegScript(string name, params RegistryScriptVersion[] versions)
        {
            return new RegistryScript
            {
                Name = name,
                Versions = versions.ToList()
            };
        }

        internal static RegistryScriptVersion RegVer(string version, params RegistryFile[] files)
        {
            return new RegistryScriptVersion
            {
                Version = version,
                Files = files.ToList()
            };
        }

        internal static RegistryFile RegFile(string filename, string hash, string url)
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
            private readonly List<string> _errors = new List<string>();

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

            internal SavesMapResult Build()
            {
                return new SavesMapResult
                {
                    ScriptsByFilename = _scripts.ToDictionary(s => s.FullPath, s => s),
                    Scenes = _scenes.ToArray(),
                    Errors = _errors.ToArray()
                };
            }
        }
        #endregion
    }
}
