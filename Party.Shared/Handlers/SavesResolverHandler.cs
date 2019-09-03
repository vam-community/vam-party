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
            if (filters is null) throw new ArgumentNullException(nameof(filters));
            if (filters.Any(p => !_fs.Path.IsPathRooted(p))) throw new ArgumentException("All filter paths must be rooted", nameof(filters));
            if (filters.Any(p => !_fs.File.Exists(p))) throw new ArgumentException("All files must exist", nameof(filters));

            var canonicalPaths = filters.Select(p => _fs.Directory.GetFiles(_fs.Path.GetDirectoryName(p), _fs.Path.GetFileName(p), SearchOption.TopDirectoryOnly).First());
            var filterPaths = (await Task.WhenAll(canonicalPaths.Select(f => _fs.Path.GetExtension(f) == ".cslist" ? ExpandCsList(f) : Task.FromResult(new[] { f }))).ConfigureAwait(false)).SelectMany(x => x).ToArray();
            var filterExtensions = canonicalPaths.Select(f => _fs.Path.GetExtension(f)).ToArray();
            bool filtering = filterExtensions.Any();
            bool filterScenes = filtering && filterExtensions.All(f => f == ".json");
            bool filterScripts = filtering && filterExtensions.All(f => f == ".cs");

            var scriptTasks = new List<Task<Script>>();
            var sceneFiles = new List<string>();
            var scriptListFiles = new List<string>();
            var vamDirectory = _fs.DirectoryInfo.FromDirectoryName(_savesDirectory).Parent.FullName;

            // TODO: If the item is a script, check all scenes
            // TODO: If the item is a script list, get scripts and all scenes
            // TODO: If the item is a scene, load the scripts/script lists from the scene

            foreach (var file in _fs.Directory.EnumerateFiles(_savesDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (_ignoredPaths.Any(ignoredPath => file.StartsWith(ignoredPath))) continue;

                switch (Path.GetExtension(file))
                {
                    case ".json":
                        if (filterScenes && !filterPaths.Contains(file)) continue;
                        sceneFiles.Add(file);
                        break;
                    case ".cs":
                        if (filterScripts && !filterPaths.Contains(file)) continue;
                        scriptTasks.Add(Task.Run(async () =>
                        {
                            return new Script(file, await Hashing.GetHashAsync(_fs, file).ConfigureAwait(false));
                        }));
                        break;
                    case ".cslist":
                        if (filterScripts && !filterPaths.Contains(file)) continue;
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
                var scriptRefPaths = await _scriptListSerializer.GetScriptsAsync(scriptListFile);
                foreach (var scriptRefRelativePath in scriptRefPaths)
                {
                    string fullPath = GetScriptListReferenceFullPath(scriptListFile, scriptRefRelativePath);
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
                try
                {
                    foreach (var scriptRefRelativePath in await _sceneSerializer.GetScriptsAsync(sceneFile).ConfigureAwait(false))
                    {
                        var fullPath = scriptRefRelativePath.Contains('/')
                            ? Path.GetFullPath(scriptRefRelativePath, vamDirectory)
                            : Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(sceneFile));
                        if (scriptsByFilename.TryGetValue(fullPath, out var scriptRef))
                        {
                            scene.References(scriptRef);
                            scriptRef.ReferencedBy(scene);
                        }
                        else if (!filterScripts)
                        {
                            errors.Add($"Scene '{sceneFile}' references a script that does not exist: '{fullPath}'");
                        }
                    }
                }
                catch (SavesException exc)
                {
                    errors.Add(exc.Message);
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

        private static string GetScriptListReferenceFullPath(string scriptListFile, string scriptRefRelativePath)
        {
            return Path.GetFullPath(scriptRefRelativePath, Path.GetDirectoryName(scriptListFile));
        }

        private async Task<string[]> ExpandCsList(string path)
        {
            var references = await _scriptListSerializer.GetScriptsAsync(path);
            return references.Select(p => GetScriptListReferenceFullPath(path, p)).ToArray();
        }
    }
}
