using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Registries;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class PackageInstalledInfoHandler
    {
        private readonly IFileSystem _fs;
        private readonly IFoldersHelper _folders;

        public PackageInstalledInfoHandler(IFileSystem fs, IFoldersHelper folders)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _folders = folders ?? throw new ArgumentNullException(nameof(folders));
        }

        public async Task<LocalPackageInfo> GetInstalledPackageInfoAsync(RegistryPackageVersionContext context)
        {
            var (registry, package, version) = context;
            var packagePath = _folders.GetDirectory(context);
            var files = new List<InstalledFileInfo>();

            foreach (var file in version.Files.Where(f => !f.Ignore && f.Filename != null))
            {
                files.Add(await GetPackageFileInfo(packagePath, file).ConfigureAwait(false));
            }
            // TODO: Handle deep dependencies
            if (version.Dependencies != null)
            {
                foreach (var dependency in version.Dependencies)
                {
                    if (!registry.TryGetDependency(dependency, out var depContext))
                        throw new RegistryException($"Could not find dependency {dependency}");
                    var depPath = _folders.GetDirectory(depContext.Package.Type);
                    var depFiles = dependency.Files != null && dependency.Files.Count > 0
                        ? dependency.Files.Select(df => depContext.Version.Files.FirstOrDefault(vf => df == vf.Filename) ?? throw new RegistryException($"Could not find dependency file '{df}' in package '{depContext.Package}@{depContext.Version.Version}'"))
                        : depContext.Version.Files;
                    foreach (var depFile in depFiles)
                        files.Add(await GetPackageFileInfo(depPath, depFile).ConfigureAwait(false));
                }
            }

            return new LocalPackageInfo
            {
                PackageFolder = packagePath,
                Files = files.ToArray(),
                Corrupted = files.Any(f => f.Status == FileStatus.HashMismatch),
                Installed = files.All(f => f.Status == FileStatus.Installed),
                Installable = files.All(f => f.Status != FileStatus.NotInstallable && f.Status != FileStatus.HashMismatch)
            };
        }

        private async Task<InstalledFileInfo> GetPackageFileInfo(string packagePath, RegistryFile file)
        {
            if (!RegistryFile.ValidFilename.IsMatch(file.Filename))
                throw new UnauthorizedAccessException($"Only files relative to the package (file.cs) or to vam (/file.cs) are accepted. Value: '{file.Filename}'");
            var fullPath = file.Filename.StartsWith("/")
                ? Path.Combine(_folders.RelativeToVam(packagePath))
                : Path.Combine(packagePath, file.Filename);

            return new InstalledFileInfo
            {
                FullPath = fullPath,
                RegistryFile = file,
                Status = await GetFileStatusAsync(file, fullPath).ConfigureAwait(false)
            };
        }

        private async Task<FileStatus> GetFileStatusAsync(RegistryFile file, string fullPath)
        {
            if (!_fs.File.Exists(fullPath))
            {
                if (file.Ignore)
                    return FileStatus.Ignored;
                if (file.Url == null)
                    return FileStatus.NotInstallable;
                return FileStatus.NotInstalled;
            }

            if (file.Hash?.Type == null)
                return FileStatus.Installed;

            if (file.Hash.Type != Hashing.Type)
                throw new InvalidOperationException($"Unsupported hash type: {file.Hash.Type}");

            var hash = await Hashing.GetHashAsync(_fs, fullPath);
            return hash == file.Hash.Value
                ? FileStatus.Installed
                : FileStatus.HashMismatch;
        }
    }
}
