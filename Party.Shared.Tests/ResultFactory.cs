using System.Collections.Generic;
using System.Linq;
using Party.Shared.Resources;
using Party.Shared.Results;

namespace Party.Shared
{
    public static class ResultFactory
    {
        #region Registry
        public static RegistryResult Reg(params RegistryScript[] scripts)
        {
            return new RegistryResult
            {
                Scripts = scripts.ToList()
            };
        }

        public static RegistryScript RegScript(string name, params RegistryScriptVersion[] versions)
        {
            return new RegistryScript
            {
                Name = name,
                Versions = versions.ToList()
            };
        }

        public static RegistryScriptVersion RegVer(string version, params RegistryFile[] files)
        {
            return new RegistryScriptVersion
            {
                Version = version,
                Files = files.ToList()
            };
        }

        public static RegistryFile RegFile(string filename, string hash, string url)
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
        public static SavesMapBuilder SavesMap()
        {
            return new SavesMapBuilder();
        }

        public static (SavesMapResult, Script) SavesMap(Script script, params Scene[] scenes)
        {
            var map = new Dictionary<string, Script>();
            script.Scenes.AddRange(scenes);
            map.Add(script.FullPath, script);
            return (new SavesMapResult
            {
                ScriptsByFilename = map,
                Scenes = scenes
            }, script);
        }

        public class SavesMapBuilder
        {
            private readonly List<Script> _scripts = new List<Script>();
            private readonly List<Scene> _scenes = new List<Scene>();
            private readonly List<string> _errors = new List<string>();

            public SavesMapBuilder WithScript(Script script, out Script outScript)
            {
                _scripts.Add(script);
                outScript = script;
                return this;
            }

            public SavesMapBuilder Referencing(Scene scene, out Scene outScene)
            {
                _scripts.Last().Scenes.Add(scene);
                _scenes.Add(scene);
                outScene = scene;
                return this;
            }

            public SavesMapBuilder WithScene(Scene scene)
            {
                _scenes.Add(scene);
                return this;
            }

            public SavesMapResult Build()
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
