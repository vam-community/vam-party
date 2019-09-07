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
        private class LoadingQueues
        {
            public Queue<string> SceneFiles { get; set; }
            public Queue<string> ScriptListFiles { get; set; }
        }

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

        public SavesResolverHandler(IFileSystem fs, ISceneSerializer sceneSerializer, IScriptListSerializer scriptListSerializer, string savesDirectory, string[] ignoredPaths)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _sceneSerializer = sceneSerializer ?? throw new ArgumentNullException(nameof(sceneSerializer));
            _scriptListSerializer = scriptListSerializer ?? throw new ArgumentNullException(nameof(scriptListSerializer));
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
            _ignoredPaths = ignoredPaths?.Select(path => Path.GetFullPath(path, savesDirectory)).ToArray() ?? new string[0];
        }

        public async Task<SavesMap> AnalyzeSaves(string[] filters)
        {
            IEnumerable<Task<Script>> scriptTasks;
            LoadingQueues queues;

            var hasFilters = filters != null && filters.Length > 0;
            if (hasFilters)
                (scriptTasks, queues) = BuildListFromFilters(filters);
            else
                (scriptTasks, queues) = BuildListFromSaves();

            var vamDirectory = _fs.DirectoryInfo.FromDirectoryName(_savesDirectory).Parent.FullName;
            var scripts = (await Task.WhenAll(scriptTasks).ConfigureAwait(false)).ToDictionary(x => x.FullPath, x => x);
            var errors = new List<(string file, string error)>();

            if (queues.ScriptListFiles != null)
            {
                var scriptListTasks = queues.ScriptListFiles.Select(file => LoadScriptList(scripts, errors, file, hasFilters));
                var scriptLists = await Task.WhenAll(scriptListTasks).ConfigureAwait(false);
                foreach (var scriptList in scriptLists.Where(sl => sl != null))
                {
                    scripts.TryAdd(scriptList.FullPath, scriptList);
                }
            }

            var sceneTasks = queues.SceneFiles.Select(file => LoadScene(scripts, vamDirectory, errors, file, hasFilters));
            var scenes = await Task.WhenAll(sceneTasks).ConfigureAwait(false);

            return new SavesMap
            {
                Errors = errors.ToArray(),
                Scripts = scripts.Values.Distinct().ToArray(),
                Scenes = scenes.ToArray()
            };
        }

        private async Task<Scene> LoadScene(IDictionary<string, Script> scripts, string vamDirectory, List<(string file, string error)> errors, string sceneFile, bool hasScriptsFilter)
        {
            var scene = new Scene(sceneFile);
            try
            {
                foreach (var scriptRefRelativePath in await _sceneSerializer.GetScriptsAsync(sceneFile).ConfigureAwait(false))
                {
                    var fullPath = scriptRefRelativePath.Contains('/')
                        ? Path.GetFullPath(scriptRefRelativePath, vamDirectory)
                        : Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(sceneFile));
                    if (scripts.TryGetValue(fullPath, out var scriptRef))
                    {
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else if (hasScriptsFilter)
                    {
                        // TODO: This has possible race conditions, but only if more than one scene in filters
                        scriptRef = await LoadScript(fullPath).ConfigureAwait(false);
                        scripts.Add(fullPath, scriptRef);
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else
                    {
                        errors.Add((sceneFile, $"Script does not exist: '{fullPath}'"));
                    }
                }
            }
            catch (SavesException exc)
            {
                errors.Add((sceneFile, exc.Message));
            }
            return scene;
        }

        private async Task<ScriptList> LoadScriptList(IDictionary<string, Script> scripts, List<(string file, string error)> errors, string scriptListFile, bool hasScriptsFilter)
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
                else if (hasScriptsFilter)
                {
                    var script = await LoadScript(fullPath);
                    scriptRefs.Add(script);
                }
                else
                {
                    errors.Add((scriptListFile, $"Script that does not exist: '{fullPath}'"));
                    scriptRefs = null;
                    break;
                }
            }

            if (scriptRefs == null || scriptRefs.Count <= 0)
                return null;

            return new ScriptList(scriptListFile, Hashing.GetHash(scriptRefPaths), scriptRefs.ToArray());
        }

        private (IEnumerable<Task<Script>>, LoadingQueues) BuildListFromSaves()
        {
            var queues = new LoadingQueues
            {
                SceneFiles = new Queue<string>(),
                ScriptListFiles = new Queue<string>()
            };
            var scriptTasks = new List<Task<Script>>();
            foreach (var file in _fs.Directory.EnumerateFiles(_savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                switch (Path.GetExtension(file))
                {
                    case ".json":
                        queues.SceneFiles.Enqueue(file);
                        break;
                    case ".cs":
                        scriptTasks.Add(LoadScript(file));
                        break;
                    case ".cslist":
                        queues.ScriptListFiles.Enqueue(file);
                        break;
                    default:
                        // Ignore anything else
                        break;
                }
            }
            return (scriptTasks, queues);
        }

        private (IEnumerable<Task<Script>>, LoadingQueues) BuildListFromFilters(string[] filters)
        {
            if (filters is null) throw new ArgumentNullException(nameof(filters));
            if (filters.Any(p => !_fs.Path.IsPathRooted(p))) throw new ArgumentException("All filter paths must be rooted", nameof(filters));
            if (filters.Any(p => !_fs.File.Exists(p))) throw new ArgumentException("All files must exist", nameof(filters));

            var canonicalPaths = filters.Select(p => _fs.Directory.GetFiles(_fs.Path.GetDirectoryName(p), _fs.Path.GetFileName(p), SearchOption.TopDirectoryOnly).First());
            IEnumerable<Task<Script>> scriptTasks = null;
            var queues = new LoadingQueues();
            foreach (var filterGroup in filters.GroupBy(_fs.Path.GetExtension))
            {
                switch (filterGroup.Key)
                {
                    case ".json":
                        queues.SceneFiles = new Queue<string>(filterGroup.ToHashSet());
                        break;
                    case ".cs":
                        scriptTasks = filterGroup.Select(LoadScript).ToList();
                        break;
                    case ".cslist":
                        queues.ScriptListFiles = new Queue<string>(filterGroup.ToHashSet());
                        break;
                    default:
                        throw new NotSupportedException($"Unknown filter extension: {filterGroup.Key}");
                }
            }
            return (scriptTasks ?? new Task<Script>[0], queues);
        }

        private Task<Script> LoadScript(string file) => Task.Run(async () => new Script(file, await Hashing.GetHashAsync(_fs, file).ConfigureAwait(false)));

        private string GetScriptListReferenceFullPath(string scriptListFile, string scriptRefRelativePath)
        {
            return Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(scriptListFile));
        }
    }
}
