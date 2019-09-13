using System.Collections.Generic;
using System.Linq;
using Party.Shared.Models;
using Party.Shared.Models.Registries;

namespace Party.Shared
{
    internal static class ResultFactory
    {
        #region Registry
        internal static Registry Reg(params RegistryPackage[] scripts)
        {
            return new Registry
            {
                Packages = new RegistryPackageGroups
                {
                    Scripts = new SortedSet<RegistryPackage>(scripts)
                }
            };
        }

        internal static RegistryPackage RegScript(string name, params RegistryPackageVersion[] versions)
        {
            return new RegistryPackage
            {
                Name = name,
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
