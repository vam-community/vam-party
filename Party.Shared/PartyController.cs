using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Resources;
using Party.Shared.Serializers;

namespace Party.Shared
{
    public class PartyController : IPartyController
    {
        private static string Version { get; } = typeof(PartyController).Assembly.GetName().Version.ToString();
        private readonly PartyConfiguration _config;
        private readonly HttpClient _http;
        private readonly IFileSystem _fs;

        private string SavesDirectory => Path.Combine(_config.VirtAMate.VirtAMateInstallFolder, "Saves");

        public PartyController(PartyConfiguration config)
        {
            _config = config;
            _fs = new FileSystem();
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Party", Version));
        }

        public Task<Registry> GetRegistryAsync(params string[] registries)
        {
            return new RegistryHandler(_http, _config.Registry.Urls)
                .AcquireAsync(registries);
        }

        public Task<SavesMap> GetSavesAsync(string[] filters)
        {
            return new SavesResolverHandler(
                _fs,
                new SceneSerializer(_fs),
                new ScriptListSerializer(_fs),
                SavesDirectory,
                _config.Scanning.Ignore)
                    .AnalyzeSaves(filters ?? new string[0]);
        }

        public Task<List<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string name, string path)
        {
            return new RegistryFilesFromPathHandler(SavesDirectory, _fs)
                .BuildFiles(registry, name, path);
        }

        public Task<List<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, string name, Uri url)
        {
            return new RegistryFilesFromUrlHandler(_http)
                .BuildFiles(registry, name, url);
        }

        public IEnumerable<SearchResult> Search(Registry registry, SavesMap saves, string query)
        {
            return new SearchHandler(_config)
                .Search(registry, saves, query);
        }

        public Task<InstalledPackageInfoResult> GetInstalledPackageInfoAsync(string name, RegistryScriptVersion version)
        {
            return new PackageStatusHandler(_fs, SavesDirectory, _config.Scanning.PackagesFolder)
                .GetInstalledPackageInfoAsync(name, version);
        }

        public Task<InstalledPackageInfoResult> InstallPackageAsync(InstalledPackageInfoResult info)
        {
            return new InstallPackageHandler(_fs, _http)
                .InstallPackageAsync(info);
        }
        public RegistrySavesMatch[] MatchSavesToRegistry(SavesMap saves, Registry registry)
        {
            return new RegistrySavesMatchHandler()
                .Match(saves, registry);
        }

        public Task<(string before, string after)[]> UpdateScriptInSceneAsync(Scene scene, Script local, InstalledPackageInfoResult info)
        {
            return new SceneUpdateHandler(new SceneSerializer(_fs), SavesDirectory)
                .UpdateScripts(scene, local, info);
        }

        public string GetDisplayPath(string path)
        {
            return GetRelativePath(path, SavesDirectory);
        }

        public string GetRelativePath(string path, string parentPath)
        {
            path = SanitizePath(path);
            if (!path.StartsWith(parentPath))
                throw new UnauthorizedAccessException($"Only paths under '{parentPath}' are allowed: '{path}'");

            return path.Substring(parentPath.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        public void SaveToFile(string data, string path, bool restrict)
        {
            if (restrict)
                path = SanitizePath(path);

            _fs.File.WriteAllText(path, data);
        }

        public void Delete(string path)
        {
            path = SanitizePath(path);
            if (!_fs.File.Exists(path))
                return;

            _fs.File.Delete(path);

            while (true)
            {
                path = _fs.Path.GetDirectoryName(path);
                if (path == null) return;
                if (!path.StartsWith(SavesDirectory)) return;
                if (_fs.Directory.EnumerateFileSystemEntries(path).Any()) return;
                _fs.Directory.Delete(path);
            }
        }

        public bool Exists(string path)
        {
            return _fs.File.Exists(SanitizePath(path));
        }

        private string SanitizePath(string path)
        {
            path = Path.GetFullPath(path, _config.VirtAMate.VirtAMateInstallFolder);
            if (!path.StartsWith(_config.VirtAMate.VirtAMateInstallFolder)) throw new UnauthorizedAccessException($"Cannot delete file {path} because it is not in the Virt-A-Mate installation folder.");
            var localPath = path.Substring(_config.VirtAMate.VirtAMateInstallFolder.Length).TrimStart(new[] { '/', '\\' });
            var directorySeparatorIndex = localPath.IndexOf('\\');
            if (directorySeparatorIndex == -1) throw new UnauthorizedAccessException($"Cannot access files directly at Virt-A-Mate's root");
            var subFolder = localPath.Substring(0, directorySeparatorIndex);
            if (!_config.VirtAMate.VirtAMateAllowedSubfolders.Contains(subFolder)) throw new UnauthorizedAccessException($"Virt-A-Mate subfolder {subFolder} is not allowed");
            return path;
        }
    }

    public interface IPartyController
    {
        Task<Registry> GetRegistryAsync(params string[] registries);
        Task<SavesMap> GetSavesAsync(string[] items = null);
        Task<List<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string name, string path);
        Task<List<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, string name, Uri url);
        IEnumerable<SearchResult> Search(Registry registry, SavesMap saves, string query);
        Task<InstalledPackageInfoResult> GetInstalledPackageInfoAsync(string name, RegistryScriptVersion version);
        Task<InstalledPackageInfoResult> InstallPackageAsync(InstalledPackageInfoResult info);
        RegistrySavesMatch[] MatchSavesToRegistry(SavesMap saves, Registry registry);
        Task<(string before, string after)[]> UpdateScriptInSceneAsync(Scene scene, Script local, InstalledPackageInfoResult info);
        string GetDisplayPath(string fullPath);
        string GetRelativePath(string fullPath, string parentPath);
        void SaveToFile(string data, string path, bool restrict = true);
        void Delete(string fullPath);
        bool Exists(string localPath);
    }
}
