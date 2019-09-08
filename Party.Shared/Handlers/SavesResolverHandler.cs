using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Resources;
using Party.Shared.Serializers;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class SavesResolverHandler
    {
        private class Filters
        {
            public HashSet<string> Scenes { get; set; }
            public HashSet<string> Scripts { get; set; }
            public HashSet<string> ScriptLists { get; set; }
        }

        private readonly IFileSystem _fs;
        private readonly ISceneSerializer _sceneSerializer;
        private readonly IScriptListSerializer _scriptListSerializer;
        private readonly string _savesDirectory;
        private readonly string[] _ignoredPaths;
        private readonly string _vamDirectory;

        public SavesResolverHandler(IFileSystem fs, ISceneSerializer sceneSerializer, IScriptListSerializer scriptListSerializer, string vamDirectory, string savesDirectory, string[] ignoredPaths)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _sceneSerializer = sceneSerializer ?? throw new ArgumentNullException(nameof(sceneSerializer));
            _scriptListSerializer = scriptListSerializer ?? throw new ArgumentNullException(nameof(scriptListSerializer));
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
            _ignoredPaths = ignoredPaths?.Select(path => Path.GetFullPath(path, savesDirectory)).ToArray() ?? new string[0];
            _vamDirectory = vamDirectory;
        }

        public Task<SavesMap> AnalyzeSaves(string filter)
        {
            var filterExt = _fs.Path.GetExtension(filter) ?? string.Empty;
            if (filterExt == string.Empty)
                return AnalyzeSavesByDirectory(filter);
            else if (filterExt == ".json")
                return AnalyzeSavesByScene(filter);
            else if (filterExt == ".cs" || filterExt == ".cslist")
                return AnalyzeSavesByScript(filter);
            else
                throw new NotSupportedException($"Filter '{filter}' is not supported");
        }

        private async Task<SavesMap> AnalyzeSavesByScript(string scriptFile)
        {
            var scripts = new Dictionary<string, Script>();
            var errors = new List<SavesError>();
            var sceneTasks = new Queue<Task<Scene>>();
            foreach (var file in _fs.Directory.EnumerateFiles(_savesDirectory, "*.json", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                sceneTasks.Enqueue(LoadScene(scripts, errors, file, true));
            }
            var scenes = await Task.WhenAll(sceneTasks);

            if (!scripts.TryGetValue(scriptFile, out var script))
            {
                script = await LoadScript(scriptFile);
            }

            return new SavesMap
            {
                Scripts = new[] { script },
                Scenes = scenes.Where(s => s.Scripts.Contains(script)).ToArray(),
                Errors = errors.ToArray()
            };
        }

        private async Task<SavesMap> AnalyzeSavesByScene(string sceneFile)
        {
            var scripts = new Dictionary<string, Script>();
            var errors = new List<SavesError>();
            // TODO: ScriptList handling
            var scene = await LoadScene(scripts, errors, sceneFile, true);
            return new SavesMap
            {
                Scenes = new[] { scene },
                Scripts = scripts.Values.ToArray(),
                Errors = errors.ToArray()
            };
        }

        private async Task<SavesMap> AnalyzeSavesByDirectory(string directory)
        {
            var sceneFiles = new Queue<string>();
            var scriptListFiles = new Queue<string>();
            var scriptTasks = new List<Task<Script>>();
            if (directory != null)
            {
                directory = _fs.Directory.GetFiles(_fs.Path.GetDirectoryName(directory), _fs.Path.GetFileName(directory), SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (directory == null) throw new UserInputException("There was no files in the specified directory");
            }
            var shouldTryLoadingReferences = directory != null;
            foreach (var file in _fs.Directory.EnumerateFiles(directory ?? _savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                // TODO: Here we should simply check if files match the filters, since we have to iterate anyway...
                switch (Path.GetExtension(file))
                {
                    case ".json":
                        sceneFiles.Enqueue(file);
                        break;
                    case ".cs":
                        scriptTasks.Add(LoadScript(file));
                        break;
                    case ".cslist":
                        scriptListFiles.Enqueue(file);
                        break;
                    default:
                        // Ignore anything else
                        break;
                }
            }

            var scripts = (await Task.WhenAll(scriptTasks).ConfigureAwait(false)).ToDictionary(x => x.FullPath, x => x);
            var errors = new List<SavesError>();

            if (scriptListFiles != null)
            {
                var scriptListTasks = scriptListFiles.Select(file => LoadScriptList(scripts, errors, file, shouldTryLoadingReferences));
                var scriptLists = await Task.WhenAll(scriptListTasks).ConfigureAwait(false);
                foreach (var scriptList in scriptLists.Where(sl => sl != null))
                {
                    scripts.TryAdd(scriptList.FullPath, scriptList);
                }
            }

            var sceneTasks = sceneFiles.Select(file => LoadScene(scripts, errors, file, shouldTryLoadingReferences));
            var scenes = await Task.WhenAll(sceneTasks).ConfigureAwait(false);

            return new SavesMap
            {
                Errors = errors.ToArray(),
                Scripts = scripts.Values.Distinct().ToArray(),
                Scenes = scenes.ToArray()
            };
        }

        private async Task<Scene> LoadScene(IDictionary<string, Script> scripts, List<SavesError> errors, string sceneFile, bool shouldTryLoadingReferences)
        {
            var scene = new Scene(sceneFile);
            try
            {
                foreach (var scriptRefRelativePath in await _sceneSerializer.GetScriptsAsync(sceneFile).ConfigureAwait(false))
                {
                    var fullPath = scriptRefRelativePath.Contains('/')
                        ? Path.GetFullPath(scriptRefRelativePath, _vamDirectory)
                        : Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(sceneFile));
                    if (scripts.TryGetValue(fullPath, out var scriptRef))
                    {
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else if (shouldTryLoadingReferences)
                    {
                        // TODO: This has possible race conditions, but only if more than one scene in filters
                        scriptRef = await LoadScript(fullPath).ConfigureAwait(false);
                        scripts.TryAdd(fullPath, scriptRef);
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else
                    {
                        errors.Add(new SavesError(sceneFile, $"Script does not exist: '{fullPath}'"));
                    }
                }
            }
            catch (SavesException exc)
            {
                errors.Add(new SavesError(sceneFile, exc.Message));
            }
            return scene;
        }

        private async Task<ScriptList> LoadScriptList(IDictionary<string, Script> scripts, List<SavesError> errors, string scriptListFile, bool shouldTryLoadingReferences)
        {
            var scriptRefs = new List<Script>();
            var scriptRefPaths = await _scriptListSerializer.GetScriptsAsync(scriptListFile);
            foreach (var scriptRefRelativePath in scriptRefPaths)
            {
                string fullPath = GetScriptListReferenceFullPath(scriptListFile, scriptRefRelativePath);
                if (scripts.TryGetValue(fullPath, out var scriptRef))
                {
                    scripts.Remove(fullPath);
                    scriptRefs.Add(scriptRef);
                }
                else if (shouldTryLoadingReferences)
                {
                    var script = await LoadScript(fullPath);
                    scriptRefs.Add(script);
                }
                else
                {
                    errors.Add(new SavesError(scriptListFile, $"Script that does not exist: '{fullPath}'"));
                    scriptRefs = null;
                    break;
                }
            }

            if (scriptRefs == null || scriptRefs.Count <= 0)
                return null;

            return new ScriptList(scriptListFile, Hashing.GetHash(scriptRefPaths), scriptRefs.ToArray());
        }

        private Task<Script> LoadScript(string file) => Task.Run(async () => new Script(file, await Hashing.GetHashAsync(_fs, file).ConfigureAwait(false)));

        private string GetScriptListReferenceFullPath(string scriptListFile, string scriptRefRelativePath)
        {
            return Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(scriptListFile));
        }
    }
}
