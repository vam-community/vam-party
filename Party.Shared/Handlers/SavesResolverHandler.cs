using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Resources;
using Party.Shared.Models;
using Party.Shared.Serializers;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class SavesResolverHandler
    {
        private readonly IFileSystem _fs;
        private readonly string _savesDirectory;
        private readonly string[] _ignoredPaths;

        public SavesResolverHandler(IFileSystem fs, string savesDirectory, string[] ignoredPaths)
        {
            _fs = fs ?? throw new System.ArgumentNullException(nameof(fs));
            _savesDirectory = savesDirectory ?? throw new System.ArgumentNullException(nameof(savesDirectory));
            _ignoredPaths = ignoredPaths?.Select(path => Path.GetFullPath(path, savesDirectory)).ToArray() ?? new string[0];
        }

        public async Task<SavesMap> AnalyzeSaves()
        {
            var scriptTasks = new List<Task<Script>>();
            var sceneFiles = new List<string>();
            var scriptListFiles = new List<string>();
            var vamDirectory = _fs.DirectoryInfo.FromDirectoryName(_savesDirectory).Parent.FullName;

            var scriptListSerializer = new ScriptListSerializer();
            var sceneSerializer = new SceneSerializer();

            foreach (var file in _fs.Directory.EnumerateFiles(_savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                switch (Path.GetExtension(file))
                {
                    case ".json":
                        sceneFiles.Add(file);
                        break;
                    case ".cs":
                        scriptTasks.Add(Task.Run(async () =>
                        {
                            return new Script(file, await Hashing.GetHashAsync(_fs, file).ConfigureAwait(false));
                        }));
                        break;
                    case ".cslist":
                        scriptListFiles.Add(file);
                        break;
                    default:
                        // Ignore anything else
                        break;
                }
            }

            var scriptsByFilename = (await Task.WhenAll(scriptTasks).ConfigureAwait(false)).ToDictionary(x => x.FullPath, x => x);
            var scenes = new List<Scene>();
            var errors = new List<string>();

            foreach (var scriptListFile in scriptListFiles)
            {
                var scriptRefs = new List<Script>();
                var scriptRefPaths = await scriptListSerializer.GetScriptsAsync(_fs, scriptListFile);
                foreach (var scriptRefRelativePath in scriptRefPaths)
                {
                    var fullPath = Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(scriptListFile));
                    if (scriptsByFilename.TryGetValue(fullPath, out var scriptRef))
                    {
                        scriptsByFilename.Remove(fullPath);
                        scriptRefs.Add(scriptRef);
                    }
                    else
                    {
                        errors.Add($"Script list '{scriptListFile}' references a script that does not exist: '{fullPath}'");
                        scriptRefs = null;
                        break;
                    }
                }
                if (scriptRefs != null)
                {
                    var scriptList = new ScriptList(scriptListFile, Hashing.GetHash(scriptRefPaths), scriptRefs.ToArray());
                    scriptsByFilename.Add(scriptListFile, scriptList);
                }
            }

            foreach (var sceneFile in sceneFiles)
            {
                var scene = new Scene(sceneFile);
                scenes.Add(scene);
                await foreach (var scriptRefRelativePath in sceneSerializer.GetScriptsAsync(_fs, sceneFile).ConfigureAwait(false))
                {
                    var fullPath = scriptRefRelativePath.Contains('/')
                        ? Path.GetFullPath(scriptRefRelativePath, vamDirectory)
                        : Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(sceneFile));
                    if (scriptsByFilename.TryGetValue(fullPath, out var scriptRef))
                    {
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else
                    {
                        errors.Add($"Scene '{sceneFile}' references a script that does not exist: '{fullPath}'");
                    }
                }
            }


            return new SavesMap
            {
                Errors = errors.ToArray(),
                // TODO: Is this dictionary really useful?
                ScriptsByFilename = scriptsByFilename,
                // TODO: This is never actually used, for now
                Scenes = scenes.ToArray()
            };
        }
    }
}
