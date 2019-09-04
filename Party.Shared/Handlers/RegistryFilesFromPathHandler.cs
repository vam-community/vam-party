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
    public class RegistryFilesFromPathHandler
    {
        private static readonly string[] _validFileExtensions = new[] { ".cs", ".cslist" };
        private readonly string _savesDirectory;
        private readonly IFileSystem _fs;

        public RegistryFilesFromPathHandler(string savesDirectory, IFileSystem fs)
        {
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        }

        public async Task<List<RegistryFile>> BuildFiles(Registry registry, string path)
        {
            if (registry is null) throw new ArgumentNullException(nameof(registry));
            if (path is null) throw new ArgumentNullException(nameof(path));

            if (!_fs.Path.IsPathRooted(path)) throw new InvalidOperationException($"Path must be rooted prior to being sent to this handler: {path}");
            if (!path.StartsWith(_savesDirectory)) throw new UserInputException($"Path must be inside the saves directory.\nPath: {path}\nSaves: {_savesDirectory}");

            var files = GetFilesFromFileSystem(path);

            if (files.Length == 0) throw new UserInputException($"No files were found with either a .cs or a .cslist extension in {path}");

            return await BuildRegistryFilesList(files);
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
