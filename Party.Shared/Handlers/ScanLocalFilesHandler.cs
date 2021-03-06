﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Logging;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Serializers;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class ScanLocalFilesHandler
    {
        private class Filters
        {
            public HashSet<string> Scenes { get; set; }
            public HashSet<string> Scripts { get; set; }
            public HashSet<string> ScriptLists { get; set; }
        }

        private readonly IFileSystem _fs;
        private readonly ILogger _logger;
        private readonly ISceneSerializer _sceneSerializer;
        private readonly IScriptListSerializer _scriptListSerializer;
        private readonly string[] _ignoredPaths;
        private readonly string _vamDirectory;
        private readonly string[] _allowedSubfolder;

        public ScanLocalFilesHandler(IFileSystem fs, ILogger logger, ISceneSerializer sceneSerializer, IScriptListSerializer scriptListSerializer, string vamDirectory, string[] allowedSubfolder, string[] ignoredPaths)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _logger = (logger ?? throw new ArgumentNullException(nameof(logger))).For("Scan");
            _vamDirectory = vamDirectory ?? throw new ArgumentNullException(nameof(vamDirectory));
            _allowedSubfolder = allowedSubfolder ?? throw new ArgumentNullException(nameof(allowedSubfolder));
            _sceneSerializer = sceneSerializer ?? throw new ArgumentNullException(nameof(sceneSerializer));
            _scriptListSerializer = scriptListSerializer ?? throw new ArgumentNullException(nameof(scriptListSerializer));
            _ignoredPaths = ignoredPaths?.Select(path => _fs.Path.GetFullPath(path, vamDirectory)).ToArray() ?? new string[0];
        }

        public Task<SavesMap> ScanLocalFilesAsync(string filter, IProgress<ScanLocalFilesProgress> reporter)
        {
            var filterExt = _fs.Path.GetExtension(filter) ?? string.Empty;
            if (filterExt == string.Empty)
                return ScanByDirectoryAsync(filter, reporter);
            else if (filterExt == ".json")
                return ScanBySceneAsync(filter);
            else if (filterExt == ".cs" || filterExt == ".cslist")
                return ScanByScriptAsync(filter, reporter);
            else
                throw new NotSupportedException($"Filter extension '{filter}' is not supported");
        }

        private async Task<SavesMap> ScanByScriptAsync(string scriptFile, IProgress<ScanLocalFilesProgress> reporter)
        {
            var scripts = new ConcurrentDictionary<string, LocalScriptFile>(StringComparer.InvariantCultureIgnoreCase);
            var sceneTasks = new Queue<Task<LocalSceneFile>>();
            int scenesCount = 0, scenesCompletedCount = 0;
            void ReportProgress()
            {
                reporter.Report(new ScanLocalFilesProgress
                {
                    Scenes = new Progress(scenesCount, scenesCompletedCount),
                    Scripts = new Progress(1, 0),
                });
            }

            foreach (var file in _fs.Directory.EnumerateFiles(_fs.Path.Combine(_vamDirectory, "Saves"), "*.json", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                Interlocked.Increment(ref scenesCount);
                sceneTasks.Enqueue(Task.Run(async () =>
                {
                    var result = await LoadSceneAsync(scripts, file, true).ConfigureAwait(false);
                    Interlocked.Increment(ref scenesCompletedCount);
                    ReportProgress();
                    return result;
                }));
            }
            var scenes = await Task.WhenAll(sceneTasks);

            if (!scripts.TryGetValue(scriptFile, out var script))
            {
                script = await LoadScriptAsync(scriptFile);
            }

            return new SavesMap
            {
                Scripts = new[] { script },
                Scenes = scenes.Where(s => s.Scripts.Contains(script)).ToArray()
            };
        }

        private IEnumerable<string> EnumerateFiles()
        {
            foreach (var subfolder in _allowedSubfolder)
            {
                var folder = _fs.Path.Combine(_vamDirectory, subfolder);
                if (!_fs.Directory.Exists(folder))
                    continue;
                foreach (var file in _fs.Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                    yield return file;
            }
        }

        private async Task<SavesMap> ScanBySceneAsync(string sceneFile)
        {
            var scripts = new ConcurrentDictionary<string, LocalScriptFile>(StringComparer.InvariantCultureIgnoreCase);
            var scene = await LoadSceneAsync(scripts, sceneFile, true);
            return new SavesMap
            {
                Scenes = new[] { scene },
                Scripts = scripts.Values.ToArray()
            };
        }

        private async Task<SavesMap> ScanByDirectoryAsync(string directory, IProgress<ScanLocalFilesProgress> reporter)
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
                reporter.Report(new ScanLocalFilesProgress
                {
                    Scenes = new Progress(scenesCount, scenesCompletedCount),
                    Scripts = new Progress(scriptsCount + scriptListsCount, scriptsCompletedCount + scriptListsCompletedCount),
                });
            }

            _logger.Verbose($"Scanning files...");

            foreach (var file in EnumerateFiles())
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;
                var savesFolder = _fs.Path.GetFullPath("Saves", _vamDirectory);

                switch (_fs.Path.GetExtension(file))
                {
                    case ".json":
                        if (!file.StartsWith(savesFolder)) continue;
                        Interlocked.Increment(ref scenesCount);
                        sceneFiles.Enqueue(file);
                        break;
                    case ".cs":
                        Interlocked.Increment(ref scriptsCount);
                        scriptTasks.Add(Task.Run(async () =>
                        {
                            var result = await LoadScriptAsync(file).ConfigureAwait(false);
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

            _logger.Verbose($"Scanning complete, waiting for scripts...");
            var scripts = new ConcurrentDictionary<string, LocalScriptFile>(
                (await Task.WhenAll(scriptTasks).ConfigureAwait(false))
                    .Select(x => new KeyValuePair<string, LocalScriptFile>(x.FullPath, x)),
                StringComparer.InvariantCultureIgnoreCase);

            if (scriptListFiles != null)
            {
                var scriptListTasks = scriptListFiles.Select(async file =>
                {
                    var result = await LoadScriptListAsync(scripts, file, shouldTryLoadingReferences);
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

            _logger.Verbose($"Scripts complete, waiting for scenes...");
            var sceneTasks = sceneFiles.Select(file => Task.Run(async () =>
            {
                var result = await LoadSceneAsync(scripts, file, shouldTryLoadingReferences).ConfigureAwait(false);
                Interlocked.Increment(ref scenesCompletedCount);
                ReportProgress();
                return result;
            }));
            var scenes = await Task.WhenAll(sceneTasks).ConfigureAwait(false);

            _logger.Verbose($"Scanning complete");
            return new SavesMap
            {
                Scripts = scripts.Values.Distinct().ToArray(),
                Scenes = scenes.ToArray()
            };
        }

        private async Task<LocalSceneFile> LoadSceneAsync(ConcurrentDictionary<string, LocalScriptFile> scripts, string sceneFile, bool shouldTryLoadingReferences)
        {
            _logger.Verbose($"{sceneFile}: Loading");
            var scene = new LocalSceneFile(sceneFile);
            try
            {
                var scriptRefs = await _sceneSerializer.FindScriptsFastAsync(sceneFile).ConfigureAwait(false);
                foreach (var scriptRefRelativePath in scriptRefs.Distinct())
                {
                    string fullPath = GetReferenceFullPath(sceneFile, scriptRefRelativePath);
                    string localPath = _fs.Path.GetFullPath(_fs.Path.GetFileName(fullPath), _fs.Path.GetDirectoryName(sceneFile));
                    _logger.Verbose($"{sceneFile}: Mapping reference {scriptRefRelativePath} to {fullPath}");

                    if (scripts.TryGetValue(localPath, out var localScriptRef))
                    {
                        scene.References(localScriptRef);
                        localScriptRef.ReferencedBy(scene);
                        _logger.Verbose($"{sceneFile}: Script {fullPath} was found in the same folder, and was already loaded by earlier scan");
                    }
                    else if (scripts.TryGetValue(fullPath, out var scriptRef))
                    {
                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);
                        _logger.Verbose($"{sceneFile}: Script {fullPath} was already loaded by earlier scan");
                    }
                    else if (_ignoredPaths.Any(p => fullPath.StartsWith(p)))
                    {
                        _logger.Verbose($"{sceneFile}: Script {fullPath} was in an ignored path");
                        continue;
                    }
                    else if (shouldTryLoadingReferences)
                    {
                        string path;
                        if (_fs.File.Exists(fullPath))
                        {
                            path = fullPath;
                        }
                        else if (_fs.File.Exists(localPath))
                        {
                            path = localPath;
                            _logger.Verbose($"{sceneFile}: Script {fullPath} was not found, but a local version was found at {localPath}");
                        }
                        else
                        {
                            scene.AddError($"Script does not exist: '{fullPath}'", LocalFileErrorLevel.Warning);
                            _logger.Warning($"{sceneFile}: Script {fullPath} does not exist");
                            continue;
                        }

                        if (_fs.Path.GetExtension(scriptRefRelativePath) == ".cslist")
                            scriptRef = await LoadScriptListAsync(scripts, fullPath, true).ConfigureAwait(false);
                        else
                            scriptRef = await LoadScriptAsync(fullPath).ConfigureAwait(false);

                        scene.References(scriptRef);
                        scriptRef.ReferencedBy(scene);

                        if (scriptRef.Status > LocalFileErrorLevel.None)
                        {
                            scene.AddError($"Script is invalid: '{fullPath}'", LocalFileErrorLevel.Warning);
                            _logger.Warning($"{sceneFile}: Script {fullPath} have errors: {string.Join(", ", scriptRef.Errors)}.");
                        }
                        else
                        {
                            scripts.TryAdd(fullPath, scriptRef);
                            _logger.Verbose($"{sceneFile}: Script {fullPath} exists and has been loaded");
                        }
                    }
                    else
                    {
                        scene.AddError($"Script does not exist: '{fullPath}'", LocalFileErrorLevel.Warning);
                        _logger.Warning($"{sceneFile}: Script {fullPath} does not exist.");
                    }
                }
            }
            catch (Exception exc)
            {
                scene.AddError(exc.Message, LocalFileErrorLevel.Error);
                _logger.Error($"{sceneFile}: {exc.Message}");
            }
            return scene;
        }

        private async Task<LocalScriptListFile> LoadScriptListAsync(IDictionary<string, LocalScriptFile> scripts, string scriptListFile, bool shouldTryLoadingReferences)
        {
            _logger.Verbose($"{scriptListFile}: Loading");
            var scriptRefs = new List<LocalScriptFile>();
            try
            {
                var scriptRefPaths = await _scriptListSerializer.GetScriptsAsync(scriptListFile);
                foreach (var scriptRefRelativePath in scriptRefPaths)
                {
                    string fullPath = GetReferenceFullPath(scriptListFile, scriptRefRelativePath);
                    if (scripts.TryGetValue(fullPath, out var scriptRef))
                    {
                        scripts.Remove(fullPath);
                        scriptRefs.Add(scriptRef);
                    }
                    else if (_ignoredPaths.Any(p => fullPath.StartsWith(p)))
                    {
                        continue;
                    }
                    else if (shouldTryLoadingReferences)
                    {
                        var script = await LoadScriptAsync(fullPath);
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
                _logger.Error($"{scriptListFile}: {exc.Message}");
                return scriptList;
            }
        }

        private async Task<LocalScriptFile> LoadScriptAsync(string scriptFile)
        {
            _logger.Verbose($"{scriptFile}: Loading");
            try
            {
                return new LocalScriptFile(scriptFile, await Hashing.GetHashAsync(_fs, scriptFile).ConfigureAwait(false));
            }
            catch (Exception exc)
            {
                var script = new LocalScriptFile(scriptFile, await Hashing.GetHashAsync(_fs, scriptFile).ConfigureAwait(false));
                script.AddError(exc.Message, LocalFileErrorLevel.Error);
                _logger.Error($"{scriptFile}: {exc.Message}");
                return script;
            }
        }

        private string GetReferenceFullPath(string containerFile, string scriptRefRelativePath)
        {
            if (scriptRefRelativePath.StartsWith("./"))
                return _fs.Path.GetFullPath(scriptRefRelativePath, _fs.Path.GetDirectoryName(containerFile));

            // VaM 1.18 path changed, but old paths are still supported
            if (scriptRefRelativePath.StartsWith("Saves/Scripts/"))
                return _fs.Path.GetFullPath("Custom/Scripts/" + scriptRefRelativePath.Substring("Saves/Scripts/".Length), _vamDirectory);

            if (scriptRefRelativePath.StartsWith("Custom/") || scriptRefRelativePath.StartsWith("Saves/"))
                return _fs.Path.GetFullPath(scriptRefRelativePath, _vamDirectory);

            return _fs.Path.GetFullPath(scriptRefRelativePath, _fs.Path.GetDirectoryName(containerFile));
        }
    }
}
