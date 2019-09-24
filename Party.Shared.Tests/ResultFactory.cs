using System.Collections.Generic;
using System.Linq;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;
using Party.Shared.Utils;

namespace Party.Shared
{
    internal static class ResultFactory
    {
        #region Registry
        internal static Registry Reg(params RegistryPackage[] scripts)
        {
            return new Registry
            {
                Packages = new SortedSet<RegistryPackage>(scripts)
            };
        }

        internal static RegistryPackage RegScript(string name, params RegistryPackageVersion[] versions)
        {
            return RegScript(name, null, versions);
        }

        internal static RegistryPackage RegScript(string name, string author, params RegistryPackageVersion[] versions)
        {
            return new RegistryPackage
            {
                Type = RegistryPackageType.Scripts,
                Name = name,
                Author = author,
                Versions = new SortedSet<RegistryPackageVersion>(versions)
            };
        }

        internal static RegistryPackageVersion RegVer(string version, params RegistryFile[] files)
        {
            return new RegistryPackageVersion
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
                Hash = new RegistryHash
                {
                    Type = Hashing.Type,
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
            private readonly List<LocalScriptFile> _scripts = new List<LocalScriptFile>();
            private readonly List<LocalSceneFile> _scenes = new List<LocalSceneFile>();

            internal SavesMapBuilder WithScript(LocalScriptFile script, out LocalScriptFile outScript)
            {
                _scripts.Add(script);
                outScript = script;
                return this;
            }

            internal SavesMapBuilder Referencing(LocalSceneFile scene, out LocalSceneFile outScene)
            {
                _scripts.Last().Scenes.Add(scene);
                _scenes.Add(scene);
                outScene = scene;
                return this;
            }

            internal SavesMapBuilder WithScene(LocalSceneFile scene)
            {
                _scenes.Add(scene);
                return this;
            }

            internal SavesMap Build()
            {
                return new SavesMap
                {
                    Scripts = _scripts.ToArray(),
                    Scenes = _scenes.ToArray()
                };
            }
        }
        #endregion
    }
}
