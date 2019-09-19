using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Models.Registries;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class BuildRegistryFilesFromPathHandler
    {
        private const int _maxScripts = 100;
        private static readonly string[] _validFileExtensions = new[] { ".cs", ".cslist" };
        private readonly IFileSystem _fs;

        public BuildRegistryFilesFromPathHandler(IFileSystem fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public async Task<SortedSet<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string path)
        {
            if (registry is null) throw new ArgumentNullException(nameof(registry));
            if (path is null) throw new ArgumentNullException(nameof(path));

            var files = GetFilesFromFileSystem(path);

            if (files.Length == 0) throw new UserInputException($"No files were found with either a .cs or a .cslist extension in {path}");

            return await BuildRegistryFilesList(files);
        }

        private async Task<SortedSet<RegistryFile>> BuildRegistryFilesList((string local, string full)[] files)
        {
            var registryFiles = new SortedSet<RegistryFile>();
            foreach (var (local, full) in files.OrderBy(s => s))
            {
                registryFiles.Add(new RegistryFile
                {
                    Filename = local,
                    Hash = new RegistryHash
                    {
                        Type = Hashing.Type,
                        Value = await Hashing.GetHashAsync(_fs, full).ConfigureAwait(false)
                    }
                });
            }
            return registryFiles;
        }

        private (string local, string full)[] GetFilesFromFileSystem(string path)
        {
            if (_fs.Directory.Exists(path))
            {
                var files = _fs.Directory
                    .EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => _validFileExtensions.Contains(_fs.Path.GetExtension(f)))
                    .Take(_maxScripts + 1)
                    .Select(f => (local: f.Substring(path.Length).Replace('\\', '/').TrimStart('/'), full: f))
                    .ToArray();

                if (files.Length > _maxScripts)
                    throw new UserInputException($"Too many files under '{path}': stopped after {_maxScripts} files");

                return files;
            }

            if (_validFileExtensions.Contains(_fs.Path.GetExtension(path)) && _fs.File.Exists(path))
                return new[] { (local: Path.GetFileName(path), full: path) };

            return new (string, string)[0];
        }
    }
}
