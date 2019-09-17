using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Local;
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
            _ignoredPaths = ignoredPaths?.Select(path => _fs.Path.GetFullPath(path, savesDirectory)).ToArray() ?? new string[0];
            _vamDirectory = vamDirectory;
        }

        public Task<SavesMap> AnalyzeSaves(string filter, IProgress<GetSavesProgress> reporter)
        {
            var filterExt = _fs.Path.GetExtension(filter) ?? string.Empty;
            if (filterExt == string.Empty)
                return AnalyzeSavesByDirectory(filter, reporter);
            else if (filterExt == ".json")
                return AnalyzeSavesByScene(filter);
            else if (filterExt == ".cs" || filterExt == ".cslist")
                return AnalyzeSavesByScript(filter, reporter);
            else
                throw new NotSupportedException($"Filter '{filter}' is not supported");
        }

        private async Task<SavesMap> AnalyzeSavesByScript(string scriptFile, IProgress<GetSavesProgress> reporter)
        {
            var scripts = new ConcurrentDictionary<string, LocalScriptFile>();
            var sceneTasks = new Queue<Task<LocalSceneFile>>();
            int scenesCount = 0, scenesCompletedCount = 0;
            void ReportProgress()
            {
                reporter.Report(new GetSavesProgress
                {
                    Scenes = new Progress(scenesCount, scenesCompletedCount),
                    Scripts = new Progress(1, 0),
                });
            }

            foreach (var file in _fs.Directory.EnumerateFiles(_savesDirectory, "*.json", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                Interlocked.Increment(ref scenesCount);
                sceneTasks.Enqueue(Task.Run(async () =>
                {
                    var result = await LoadScene(scripts, file, true).ConfigureAwait(false);
                    Interlocked.Increment(ref scenesCompletedCount);
                    ReportProgress();
                    return result;
                }));
            }
            var scenes = await Task.WhenAll(sceneTasks);

            if (!scripts.TryGetValue(scriptFile, out var script))
            {
                script = await LoadScript(scriptFile);
            }

            return new SavesMap
            {
                Scripts = new[] { script },
                Scenes = scenes.Where(s => s.Scripts.Contains(script)).ToArray()
            };
        }

        private async Task<SavesMap> AnalyzeSavesByScene(string sceneFile)
        {
            var scripts = new ConcurrentDictionary<string, LocalScriptFile>();
            // TODO: ScriptList handling
            var scene = await LoadScene(scripts, sceneFile, true);
            return new SavesMap
            {
                Scenes = new[] { scene },
                Scripts = scripts.Values.ToArray()
            };
        }

        private async Task<SavesMap> AnalyzeSavesByDirectory(string directory, IProgress<GetSavesProgress> reporter)
        {
            var sceneFiles = new Queue<string>();
            var scriptListFiles = new Queue<string>();
            var scriptTasks = new List<Task<LocalScriptFile>>();
            if (directory != null)
            {
                directory = _fs.Directory.GetFiles(_fs.Path.GetDirectoryName(directory), _fs.Path.GetFileName(directory), SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (directory == null) throw new UserInputException("There was no files in the specified directory");
            }
            var shouldTryLoadingReferences = directory != null;
            int scenesCount = 0, scenesCompletedCount = 0;
            int scriptsCount = 0, scriptsCompletedCount = 0;
            int scriptListsCount = 0, scriptListsCompletedCount = 0;
            void ReportProgress()
            {
                reporter.Report(new GetSavesProgress
                {
                    Scenes = new Progress(scenesCount, scenesCompletedCount),
                    Scripts = new Progress(scriptsCount + scriptListsCount, scriptsCompletedCount + scriptListsCompletedCount),
                });
            }

            foreach (var file in _fs.Directory.EnumerateFiles(directory ?? _savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                // TODO: Here we should simply check if files match the filters, since we have to iterate anyway...
                switch (Path.GetExtension(file))
                {
                    case ".json":
                        Interlocked.Increment(ref scenesCount);
                        sceneFiles.Enqueue(file);
                        break;
                    case ".cs":
                        Interlocked.Increment(ref scriptsCount);
                        scriptTasks.Add(Task.Run(async () =>
                        {
                            var result = await LoadScript(file).ConfigureAwait(false);
                            Interlocked.Increment(ref scriptsCompletedCount);
                            ReportProgress();
                            return result;
                        }));
                        break;
                    case ".cslist":
                        Interlocked.Increment(ref scriptListsCount);
                        scriptListFiles.Enqueue(file);
                        break;
                    default:
                        // Ignore anything else
                        break;
                }

                ReportProgress();
            }

            var scripts = new ConcurrentDictionary<string, LocalScriptFile>((await Task.WhenAll(scriptTasks).ConfigureAwait(false)).ToDictionary(x => x.FullPath, x => x));

            if (scriptListFiles != null)
            {
                var scriptListTasks = scriptListFiles.Select(async file =>
                {
                    var result = await LoadScriptList(scripts, file, shouldTryLoadingReferences);
                    Interlocked.Increment(ref scriptListsCompletedCount);
                    ReportProgress();
                    return result;
                });
                var scriptLists = await Task.WhenAll(scriptListTasks).ConfigureAwait(false);
                foreach (var scriptList in scriptLists.Where(sl => sl != null))
                {
                    scripts.TryAdd(scriptList.FullPath, scriptList);
                }
            }

            var sceneTasks = sceneFiles.Select(file => Task.Run(async () =>
            {
                var result = await LoadScene(scripts, file, shouldTryLoadingReferences).ConfigureAwait(false);
                Interlocked.Increment(ref scenesCompletedCount);
                ReportProgress();
                return result;
            }));
            var scenes = await Task.WhenAll(sceneTasks).ConfigureAwait(false);

            return new SavesMap
            {
                Scripts = scripts.Values.Distinct().ToArray(),
                Scenes = scenes.ToArray()
            };
        }

        private async Task<LocalSceneFile> LoadScene(ConcurrentDictionary<string, LocalScriptFile> scripts, string sceneFile, bool shouldTryLoadingReferences)
        {
            var scene = new LocalSceneFile(sceneFile);
            try
            {
                var json = await _sceneSerializer.Deserialize(sceneFile).ConfigureAwait(false);
                foreach (var scriptRefRelativePath in json.Atoms.SelectMany(a => a.Plugins).Select(p => p.Path))
                {
                    var fullPath = scriptRefRelativePath.Contains('/')
                        ? _fs.Path.GetFullPath(scriptRefRelativePath, _vamDirectory)
                        : _fs.Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(sceneFile));
                    if (scripts.TryGetValue(fullPath, out var scriptRef))
                    {
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else if (shouldTryLoadingReferences)
                    {
                        // TODO: This has possible race conditions, but only if more than one scene in filters
                        scriptRef = await LoadScript(fullPath).ConfigureAwait(false);
                        if (scriptRef.Status > LocalFileErrorLevel.None)
                        {
                            scene.AddError($"Script does not exist or is invalid: '{fullPath}'", LocalFileErrorLevel.Warning);
                        }
                        else
                        {
                            scripts.TryAdd(fullPath, scriptRef);
                            scene.References(scriptRef);
                            scriptRef.ReferencedBy(scene);
                        }
                    }
                    else
                    {
                        scene.AddError($"Script does not exist: '{fullPath}'", LocalFileErrorLevel.Warning);
                    }
                }
            }
            catch (Exception exc)
            {
                scene.AddError(exc.Message, LocalFileErrorLevel.Error);
            }
            return scene;
        }

        private async Task<LocalScriptListFile> LoadScriptList(IDictionary<string, LocalScriptFile> scripts, string scriptListFile, bool shouldTryLoadingReferences)
        {
            var scriptRefs = new List<LocalScriptFile>();
            try
            {
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
                        var script = new LocalScriptFile(fullPath, null);
                        script.AddError($"Script that does not exist: '{fullPath}'", LocalFileErrorLevel.Error);
                        scriptRefs.Add(script);
                    }
                }

                if (scriptRefs == null || scriptRefs.Count <= 0)
                    return null;

                var scriptsWithErrors = scriptRefs.Where(s => s.Status > LocalFileErrorLevel.None).ToList();
                var scriptList = new LocalScriptListFile(scriptListFile, Hashing.GetHash(scriptRefPaths), scriptRefs.ToArray());
                if (scriptsWithErrors.Count == 1)
                    scriptList.AddError($"Script {scriptsWithErrors[0].FullPath} has an issue: {scriptsWithErrors[0].Errors[0].Error}", scriptsWithErrors[0].Status);
                else if (scriptsWithErrors.Count > 1)
                    scriptList.AddError($"{scriptsWithErrors.Count} scripts have issues. First issue: {scriptsWithErrors[0].Errors[0].Error}", scriptsWithErrors[0].Status);

                return scriptList;
            }
            catch (Exception exc)
            {
                var scriptList = new LocalScriptListFile(scriptListFile, null, scriptRefs.ToArray());
                scriptList.AddError(exc.Message, LocalFileErrorLevel.Error);
                return scriptList;
            }
        }

        private async Task<LocalScriptFile> LoadScript(string scriptFile)
        {
            try
            {
                return new LocalScriptFile(scriptFile, await Hashing.GetHashAsync(_fs, scriptFile).ConfigureAwait(false));
            }
            catch (Exception exc)
            {
                var script = new LocalScriptFile(scriptFile, await Hashing.GetHashAsync(_fs, scriptFile).ConfigureAwait(false));
                script.AddError(exc.Message, LocalFileErrorLevel.Error);
                return script;
            }
        }

        private string GetScriptListReferenceFullPath(string scriptListFile, string scriptRefRelativePath)
        {
            return _fs.Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(scriptListFile));
        }
    }
}
