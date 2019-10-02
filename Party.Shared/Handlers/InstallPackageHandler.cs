using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class InstallPackageHandler
    {
        private readonly IFileSystem _fs;
        private readonly HttpClient _http;

        public InstallPackageHandler(IFileSystem fs, HttpClient http)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<LocalPackageInfo> InstallPackageAsync(LocalPackageInfo info, bool force)
        {
            var files = new List<InstalledFileInfo>();
            foreach (var file in info.Files.Where(f => !f.RegistryFile.Ignore))
            {
                files.Add(await InstallFileAsync(force, file).ConfigureAwait(false));
            }
            return new LocalPackageInfo
            {
                PackageFolder = info.PackageFolder,
                Files = files.ToArray(),
                Corrupted = files.Any(f => f.Status == FileStatus.HashMismatch),
                Installed = files.All(f => f.Status == FileStatus.Installed),
                Installable = files.All(f => f.Status != FileStatus.NotDownloadable && f.Status != FileStatus.HashMismatch)
            };
        }

        private async Task<InstalledFileInfo> InstallFileAsync(bool force, InstalledFileInfo file)
        {
            var directory = Path.GetDirectoryName(file.FullPath);
            if (!_fs.Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var fileResult = new InstalledFileInfo
            {
                FullPath = file.FullPath,
                RegistryFile = file.RegistryFile
            };
            if (_fs.File.Exists(file.FullPath))
            {
                if (force)
                {
                    _fs.File.Delete(file.FullPath);
                }
                else if (string.IsNullOrEmpty(file.RegistryFile.Hash?.Value))
                {
                    file.Status = FileStatus.Installed;
                    return file;
                }
                else
                {
                    return ValidateHash(force, file, fileResult, _fs.File.ReadAllText(file.FullPath));
                }
            }
            if (string.IsNullOrEmpty(file.RegistryFile.Url))
            {
                file.Status = FileStatus.NotDownloadable;
                file.Reason = $"No URL provided.";
                return file;
            }
            if (!Uri.IsWellFormedUriString(file.RegistryFile.Url, UriKind.Absolute))
            {
                file.Status = FileStatus.NotDownloadable;
                file.Reason = $"Provided file URL '{file.RegistryFile.Url}' is not a valid url.";
                return file;
            }
            var response = await _http.GetAsync(file.RegistryFile.Url).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception exc)
            {
                fileResult.Status = FileStatus.NotDownloadable;
                fileResult.Reason = $"Failed to download file from {file.RegistryFile.Url}: Received status {response.StatusCode}.\n{exc.Message}";
                return fileResult;
            }
            var content = await response.Content.ReadAsStringAsync();
            return ValidateHash(force, file, fileResult, content);
        }

        private InstalledFileInfo ValidateHash(bool force, InstalledFileInfo file, InstalledFileInfo fileResult, string content)
        {
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var hash = Hashing.GetHash(lines);
            if (!force && hash != file.RegistryFile.Hash.Value)
            {
                fileResult.Status = FileStatus.HashMismatch;
                fileResult.Reason = $"Expected hash {file.RegistryFile.Hash.Value}, received {hash}. The file was either corrupted during download, or the wrong hash was pushed to the registry.";
                return fileResult;
            }
            else
            {
                _fs.File.WriteAllText(file.FullPath, content);
                fileResult.Status = FileStatus.Installed;
                return fileResult;
            }
        }
    }
}
