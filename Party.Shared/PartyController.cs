using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Exceptions;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Serializers;

namespace Party.Shared
{
    public class PartyController : IPartyController
    {
        public bool ChecksEnabled { get; set; }

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

        public void HealthCheck()
        {
            if (!ChecksEnabled)
                return;

            if (!_fs.Directory.Exists(SavesDirectory))
                throw new SavesException($"Could not find the '{SavesDirectory}' directory under '{_config.VirtAMate.VirtAMateInstallFolder}'. Either put party.exe in your Virt-A-Mate installation folder, or specify --vam in the options.");
        }

        public Task<Registry> GetRegistryAsync(params string[] registries)
        {
            return new RegistryHandler(_http, _config.Registry.Urls)
                .AcquireAsync(registries);
        }

        public Task<SavesMap> GetSavesAsync(string filter = null)
        {
            return new SavesResolverHandler(
                _fs,
                new SceneSerializer(_fs),
                new ScriptListSerializer(_fs),
                _config.VirtAMate.VirtAMateInstallFolder,
                SavesDirectory,
                _config.VirtAMate.IgnoredFolders)
                    .AnalyzeSaves(filter);
        }

        public Task<SortedSet<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string path, DirectoryInfo saves)
        {
            return new RegistryFilesFromPathHandler(saves?.FullName ?? SavesDirectory, _fs)
                .BuildFiles(registry, saves != null ? path : SanitizePath(path));
        }

        public Task<SortedSet<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, Uri url)
        {
            return new RegistryFilesFromUrlHandler(_http)
                .BuildFiles(registry, url);
        }

        public IEnumerable<SearchResult> Search(Registry registry, SavesMap saves, string query)
        {
            return new SearchHandler(_config.Registry.TrustedDomains)
                .Search(registry, saves, query);
        }

        public Task<LocalPackageInfo> GetInstalledPackageInfoAsync(string name, RegistryPackageVersion version)
        {
            return new PackageStatusHandler(_fs, SavesDirectory, _config.VirtAMate.PackagesFolder)
                .GetInstalledPackageInfoAsync(name, version);
        }

        public Task<LocalPackageInfo> InstallPackageAsync(LocalPackageInfo info, bool force)
        {
            return new InstallPackageHandler(_fs, _http)
                .InstallPackageAsync(info, force);
        }
        public RegistrySavesMatches MatchSavesToRegistry(SavesMap saves, Registry registry)
        {
            return new RegistrySavesMatchHandler()
                .Match(saves, registry);
        }

        public Task<(string before, string after)[]> UpdateScriptInSceneAsync(Scene scene, Script local, LocalPackageInfo info)
        {
            return new SceneUpdateHandler(new SceneSerializer(_fs), SavesDirectory)
                .UpdateScripts(scene, local, info);
        }

        public async Task<string> GetPartyUpdatesAvailable()
        {
            var response = await _http.GetAsync("https://api.github.com/repos/vam-community/vam-party/releases/latest");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return new JsonSerializer().Deserialize<GitHubReleaseInfo>(jsonReader)?.Name;
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
            path = _fs.Path.GetFullPath(path, _config.VirtAMate.VirtAMateInstallFolder);
            if (ChecksEnabled)
            {
                if (!path.StartsWith(_config.VirtAMate.VirtAMateInstallFolder)) throw new UnauthorizedAccessException($"Cannot process path '{path}' because it is not in the Virt-A-Mate installation folder.");
                var localPath = path.Substring(_config.VirtAMate.VirtAMateInstallFolder.Length).TrimStart(new[] { '/', '\\' });
                var directorySeparatorIndex = localPath.IndexOf('\\');
                if (directorySeparatorIndex == -1) throw new UnauthorizedAccessException($"Cannot access files directly at Virt-A-Mate's root");
                var subFolder = localPath.Substring(0, directorySeparatorIndex);
                if (!_config.VirtAMate.AllowedSubfolders.Contains(subFolder)) throw new UnauthorizedAccessException($"Accessing Virt-A-Mate subfolder '{subFolder}' is not allowed");
            }
            return path;
        }
    }

    public interface IPartyController
    {
        bool ChecksEnabled { get; set; }

        void HealthCheck();
        Task<Registry> GetRegistryAsync(params string[] registries);
        Task<SavesMap> GetSavesAsync(string filter = null);
        Task<SortedSet<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string path, DirectoryInfo saves);
        Task<SortedSet<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, Uri url);
        IEnumerable<SearchResult> Search(Registry registry, SavesMap saves, string query);
        Task<LocalPackageInfo> GetInstalledPackageInfoAsync(string name, RegistryPackageVersion version);
        Task<LocalPackageInfo> InstallPackageAsync(LocalPackageInfo info, bool force);
        RegistrySavesMatches MatchSavesToRegistry(SavesMap saves, Registry registry);
        Task<(string before, string after)[]> UpdateScriptInSceneAsync(Scene scene, Script local, LocalPackageInfo info);
        Task<string> GetPartyUpdatesAvailable();
        string GetDisplayPath(string path);
        string GetRelativePath(string path, string parentPath);
        void SaveToFile(string data, string path, bool restrict = true);
        void Delete(string path);
        bool Exists(string path);
    }
}
