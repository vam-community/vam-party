﻿using System;
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
                using var response = await _http.GetAsync(file.RegistryFile.Url).ConfigureAwait(false);
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception exc)
                {
                    throw new RegistryException($"Failed to download file from {file.RegistryFile.Url}", exc);
                }
                var content = await response.Content.ReadAsStringAsync();
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var hash = Hashing.GetHash(lines);
                if (!force && hash != file.RegistryFile.Hash.Value)
                {
                    fileResult.Status = FileStatus.HashMismatch;
                    files.Add(fileResult);
                }
                else
                {
                    _fs.File.WriteAllText(file.FullPath, content);
                    fileResult.Status = FileStatus.Installed;
                    files.Add(fileResult);
                }
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
    }
}
