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
    public class GetInstalledPackageInfoHandler
    {
        private readonly IFileSystem _fs;
        private readonly IFoldersHelper _folders;

        public GetInstalledPackageInfoHandler(IFileSystem fs, IFoldersHelper folders)
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
            if (version.Dependencies != null)
            {
                foreach (var dependency in version.Dependencies)
                {
                    if (!registry.TryGetDependency(dependency, out var depContext))
                        throw new RegistryException($"Could not find dependency {dependency}");
                    if (depContext.Version.Dependencies != null && depContext.Version.Dependencies.Count > 0)
                        throw new RegistryException($"Nesting of dependencies is not yet supported. {context} -> {depContext} -> {depContext.Version.Dependencies.FirstOrDefault()}");
                    var depPath = _folders.GetDirectory(depContext);
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
                Installable = files.All(f => f.Status != FileStatus.NotDownloadable && f.Status != FileStatus.HashMismatch)
            };
        }

        private async Task<InstalledFileInfo> GetPackageFileInfo(string packagePath, RegistryFile file)
        {
            if (!RegistryFile.ValidFilename.IsMatch(file.Filename))
                throw new UnauthorizedAccessException($"Only files relative to the package (file.cs) or to vam (/file.cs) are accepted. Value: '{file.Filename}'");
            var fullPath = file.Filename.StartsWith("/")
                ? Path.Combine(_folders.FromRelativeToVam(file.Filename.Substring(1)))
                : Path.Combine(packagePath, file.Filename);

            var info = new InstalledFileInfo
            {
                FullPath = fullPath,
                RegistryFile = file
            };

            var (status, reason) = await GetFileStatusAsync(file, fullPath).ConfigureAwait(false);

            info.Status = status;
            info.Reason = reason;

            return info;
        }

        private async Task<(FileStatus status, string reason)> GetFileStatusAsync(RegistryFile file, string fullPath)
        {
            if (!_fs.File.Exists(fullPath))
            {
                if (file.Ignore)
                    return (FileStatus.Ignored, null);
                if (file.Url == null)
                    return (FileStatus.NotDownloadable, $"No URL provided.");
                return (FileStatus.NotInstalled, null);
            }

            if (file.Hash?.Type == null)
                return (FileStatus.Installed, null);

            if (file.Hash.Type != Hashing.Type)
                throw new InvalidOperationException($"Unsupported hash type: {file.Hash.Type}.");

            var hash = await Hashing.GetHashAsync(_fs, fullPath);
            return hash == file.Hash.Value
                ? (FileStatus.Installed, (string)null)
                : (FileStatus.HashMismatch, $"Expected hash {file.Hash.Value}, file on disk was {hash}.");
        }
    }
}
