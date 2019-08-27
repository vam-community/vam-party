using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using Party.Shared.Exceptions;
using Party.Shared.Results;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class InstallPackageHandler
    {
        private readonly IFileSystem _fs;
        private readonly HttpClient _httpClient;

        public InstallPackageHandler(IFileSystem fs, HttpClient httpClient)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<InstalledPackageInfoResult> InstallPackageAsync(InstalledPackageInfoResult info)
        {
            var files = new List<InstalledPackageInfoResult.InstalledFileInfo>();
            foreach (var file in info.Files)
            {
                var directory = Path.GetDirectoryName(file.Path);
                if (!_fs.Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var fileResult = new InstalledPackageInfoResult.InstalledFileInfo
                {
                    Path = file.Path,
                    RegistryFile = file.RegistryFile
                };
                using var response = await _httpClient.GetAsync(file.RegistryFile.Url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var hash = Hashing.GetHash(lines);
                if (hash != file.RegistryFile.Hash.Value)
                {
                    throw new PackageInstallationException($"Hash mismatch between registry file '{file.RegistryFile.Filename}' ({file.RegistryFile.Hash.Value}) and downloaded file '{file.RegistryFile.Url}' ({hash})");
                }
                _fs.File.WriteAllText(file.Path, content);
                fileResult.Status = InstalledPackageInfoResult.FileStatus.Installed;
                files.Add(fileResult);
            }
            return new InstalledPackageInfoResult
            {
                InstallFolder = info.InstallFolder,
                Files = files.ToArray()
            };
        }
    }
}
