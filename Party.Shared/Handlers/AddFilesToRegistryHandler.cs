using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class AddFilesToRegistryHandler
    {
        private static readonly string[] _validFileExtensions = new[] { ".cs", ".cslist" };
        private readonly string _savesDirectory;
        private readonly IFileSystem _fs;

        public AddFilesToRegistryHandler(string savesDirectory, IFileSystem fs)
        {
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public async Task<(RegistryScript script, RegistryScriptVersion version)> AddScriptVersionAsync(Registry registry, string name, string pathOrUrl)
        {
            if (registry is null) throw new ArgumentNullException(nameof(registry));
            if (pathOrUrl is null) throw new ArgumentNullException(nameof(pathOrUrl));

            if (!_fs.Path.IsPathRooted(pathOrUrl)) throw new InvalidOperationException($"Path must be rooted prior to being sent to this handler: {pathOrUrl}");
            if (!pathOrUrl.StartsWith(_savesDirectory)) throw new UserInputException($"Path must be inside the saves directory.\nPath: {pathOrUrl}\nSaves: {_savesDirectory}");

            var files = GetFilesFromFileSystem(pathOrUrl);

            if (files.Length == 0) throw new UserInputException($"No files were found with either a .cs or a .cslist extension in {pathOrUrl}");

            var registryFiles = await BuildRegistryFilesList(files);
            AssertNoDuplicates(registry, registryFiles);

            var script = registry.GetOrCreateScript(name);
            var version = script.CreateVersion();
            version.Files.AddRange(registryFiles);

            return (script, version);
        }

        private static void AssertNoDuplicates(Registry registry, List<RegistryFile> files)
        {
            var versionFileHashes = files.Select(f => f.Hash.Value).ToArray();
            var versionWithSameHashes = registry.Scripts
                .SelectMany(script => script.Versions.Select(version => (script, version)))
                .FirstOrDefault(x => x.version.Files.Count == versionFileHashes.Length && x.version.Files.All(f => versionFileHashes.Contains(f.Hash.Value)));

            if (versionWithSameHashes.version != null)
                throw new UserInputException($"This version contains exactly the same file count and file hashes as {versionWithSameHashes.script.Name} v{versionWithSameHashes.version.Version}.");
        }

        private async Task<List<RegistryFile>> BuildRegistryFilesList(string[] files)
        {
            var registryFiles = new List<RegistryFile>();
            foreach (var file in files.OrderBy(s => s))
            {
                registryFiles.Add(new RegistryFile
                {
                    Filename = _fs.Path.GetFileName(file),
                    Url = "",
                    Hash = new RegistryFileHash
                    {
                        Type = Hashing.Type,
                        Value = await Hashing.GetHashAsync(_fs, file).ConfigureAwait(false)
                    }
                });
            }

            return registryFiles;
        }

        private string[] GetFilesFromFileSystem(string path)
        {
            if (_fs.Directory.Exists(path))
                return _fs.Directory.GetFiles(path).Where(f => _validFileExtensions.Contains(_fs.Path.GetExtension(f))).ToArray();

            if (_validFileExtensions.Contains(_fs.Path.GetExtension(path)) && _fs.File.Exists(path))
                return new[] { path };

            return new string[0];
        }
    }
}
