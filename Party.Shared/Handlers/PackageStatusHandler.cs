using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Models;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class PackageStatusHandler
    {
        private readonly IFileSystem _fs;
        private readonly string _savesDirectory;
        private readonly string _packagesFolder;

        public PackageStatusHandler(IFileSystem fs, string savesDirectory, string packagesFolder)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _savesDirectory = savesDirectory ?? throw new ArgumentNullException(nameof(savesDirectory));
            _packagesFolder = packagesFolder ?? throw new ArgumentNullException(nameof(packagesFolder));
        }

        public async Task<LocalPackageInfo> GetInstalledPackageInfoAsync(string name, RegistryPackageVersion version)
        {
            var basePath = Path.GetFullPath(_packagesFolder, _savesDirectory);
            if (!basePath.StartsWith(_savesDirectory))
            {
                throw new UnauthorizedAccessException($"The packages folder must be within the saves directory: '{basePath}'");
            }
            var installPath = Path.Combine(basePath, name, version.Version);
            var files = new List<LocalPackageInfo.InstalledFileInfo>();
            foreach (var file in version.Files.Where(f => !f.Ignore && f.Filename != null))
            {
                files.Add(await GetPackageFileInfo(installPath, file).ConfigureAwait(false));
            }
            return new LocalPackageInfo
            {
                InstallFolder = basePath,
                Files = files.ToArray()
            };
        }

        private async Task<LocalPackageInfo.InstalledFileInfo> GetPackageFileInfo(string installPath, RegistryFile file)
        {
            var filePath = Path.Combine(installPath, file.Filename);
            var fileInfo = new LocalPackageInfo.InstalledFileInfo
            {
                Path = filePath,
                RegistryFile = file
            };
            if (_fs.File.Exists(filePath))
            {
                var hash = await Hashing.GetHashAsync(_fs, filePath);
                if (file.Hash.Type != Hashing.Type)
                {
                    throw new InvalidOperationException($"Unsupported hash type: {file.Hash.Type}");
                }
                if (hash == file.Hash.Value)
                {
                    fileInfo.Status = LocalPackageInfo.FileStatus.Installed;
                }
                else
                {
                    fileInfo.Status = LocalPackageInfo.FileStatus.HashMismatch;
                }
            }
            else if (file.Ignore)
            {
                fileInfo.Status = LocalPackageInfo.FileStatus.Ignored;
            }
            else
            {
                fileInfo.Status = LocalPackageInfo.FileStatus.NotInstalled;
            }
            return fileInfo;
        }
    }
}
