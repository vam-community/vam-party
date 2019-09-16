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
            var errors = new ConcurrentBag<SavesError>();
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
                    var result = await LoadScene(scripts, errors, file, true).ConfigureAwait(false);
                    Interlocked.Increment(ref scenesCompletedCount);
                    ReportProgress();
                    return result;
                }));
            }
            var scenes = await Task.WhenAll(sceneTasks);

            if (!scripts.TryGetValue(scriptFile, out var script))
            {
                script = await LoadScript(scriptFile, errors);
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
            var scripts = new ConcurrentDictionary<string, LocalScriptFile>();
            var errors = new ConcurrentBag<SavesError>();
            // TODO: ScriptList handling
            var scene = await LoadScene(scripts, errors, sceneFile, true);
            return new SavesMap
            {
                Scenes = new[] { scene },
                Scripts = scripts.Values.ToArray(),
                Errors = errors.ToArray()
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
            var errors = new ConcurrentBag<SavesError>();
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
                            var result = await LoadScript(file, errors).ConfigureAwait(false);
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
                    var result = await LoadScriptList(scripts, errors, file, shouldTryLoadingReferences);
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
                var result = await LoadScene(scripts, errors, file, shouldTryLoadingReferences).ConfigureAwait(false);
                Interlocked.Increment(ref scenesCompletedCount);
                ReportProgress();
                return result;
            }));
            var scenes = await Task.WhenAll(sceneTasks).ConfigureAwait(false);

            return new SavesMap
            {
                Errors = errors.ToArray(),
                Scripts = scripts.Values.Distinct().ToArray(),
                Scenes = scenes.ToArray()
            };
        }

        private async Task<LocalSceneFile> LoadScene(ConcurrentDictionary<string, LocalScriptFile> scripts, ConcurrentBag<SavesError> errors, string sceneFile, bool shouldTryLoadingReferences)
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
                        scriptRef = await LoadScript(fullPath, errors).ConfigureAwait(false);
                        scripts.TryAdd(fullPath, scriptRef);
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                    }
                    else
                    {
                        errors.Add(new SavesError(sceneFile, $"Script does not exist: '{fullPath}'", SavesErrorLevel.Warning));
                    }
                }
            }
            catch (SavesException exc)
            {
                errors.Add(new SavesError(sceneFile, exc.Message, SavesErrorLevel.Warning));
            }
            catch (Exception exc)
            {
                errors.Add(new SavesError(sceneFile, exc.Message, SavesErrorLevel.Error));
            }
            return scene;
        }

        private async Task<LocalScriptListFile> LoadScriptList(IDictionary<string, LocalScriptFile> scripts, ConcurrentBag<SavesError> errors, string scriptListFile, bool shouldTryLoadingReferences)
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
                        var script = await LoadScript(fullPath, errors);
                        scriptRefs.Add(script);
                    }
                    else
                    {
                        errors.Add(new SavesError(scriptListFile, $"Script that does not exist: '{fullPath}'", SavesErrorLevel.Warning));
                        scriptRefs = null;
                        break;
                    }
                }

                if (scriptRefs == null || scriptRefs.Count <= 0)
                    return null;

                return new LocalScriptListFile(scriptListFile, Hashing.GetHash(scriptRefPaths), scriptRefs.ToArray());
            }
            catch (Exception exc)
            {
                errors.Add(new SavesError(scriptListFile, exc.Message, SavesErrorLevel.Error));
                return new LocalScriptListFile(scriptListFile, null, scriptRefs.ToArray());
            }
        }

        private async Task<LocalScriptFile> LoadScript(string scriptFile, ConcurrentBag<SavesError> errors)
        {
            try
            {
                return new LocalScriptFile(scriptFile, await Hashing.GetHashAsync(_fs, scriptFile).ConfigureAwait(false));
            }
            catch (Exception exc)
            {
                errors.Add(new SavesError(scriptFile, exc.Message, SavesErrorLevel.Error));
                return null;
            }
        }

        private string GetScriptListReferenceFullPath(string scriptListFile, string scriptRefRelativePath)
        {
            return _fs.Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(scriptListFile));
        }
    }
}
