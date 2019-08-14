using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Resources;
using Party.Shared.Results;
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

        public async Task<SavesMapResult> AnalyzeSaves()
        {
            var scriptTasks = new List<Task<Script>>();
            var sceneFiles = new List<string>();
            var scriptListFiles = new List<string>();

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

            var scriptsDict = (await Task.WhenAll(scriptTasks).ConfigureAwait(false)).ToDictionary(x => x.FullPath, x => x);
            var scenes = new List<Scene>();
            var errors = new List<string>();

            foreach (var scriptListFile in scriptListFiles)
            {
                var scriptRefs = new List<Script>();
                var scriptRefPaths = await ScriptList.GetScriptsAsync(scriptListFile);
                foreach (var scriptRefRelativePath in scriptRefPaths)
                {
                    var fullPath = Path.GetFullPath(scriptRefRelativePath, System.IO.Path.GetDirectoryName(scriptListFile));
                    if (scriptsDict.TryGetValue(fullPath, out var scriptRef))
                    {
                        scriptsDict.Remove(fullPath);
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
                    scriptsDict.Add(scriptListFile, scriptList);
                }
            }

            foreach (var sceneFile in sceneFiles)
            {
                var scene = new Scene(sceneFile);
                await foreach (var scriptRefRelativePath in scene.GetScriptsAsync().ConfigureAwait(false))
                {
                    var fullPath = Path.GetFullPath(scriptRefRelativePath, System.IO.Path.GetDirectoryName(sceneFile));
                    if (scriptsDict.TryGetValue(fullPath, out var scriptRef))
                    {
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else
                    {
                        errors.Add($"Scene '{sceneFile}' references a script that does not exist: '{fullPath}'");
                        continue;
                    }
                }
            }


            return new SavesMapResult
            {
                IdentifierScriptMap = scriptsDict,
                Scenes = scenes.ToArray()
            };
        }
    }
}
