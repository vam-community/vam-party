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
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;
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
        private IFoldersHelper _folders;

        private string SavesDirectory => Path.Combine(_config.VirtAMate.VirtAMateInstallFolder, "Saves");

        public PartyController(PartyConfiguration config)
        {
            _config = config;
            _fs = new FileSystem();
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Party", Version));
            _folders = new FoldersHelper(_fs, _config.VirtAMate.VirtAMateInstallFolder);
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
            return new RegistryHandler(_http, _config.Registry.Urls, new RegistrySerializer())
                .AcquireAsync(registries);
        }

        public Task<SavesMap> GetSavesAsync(string filter, IProgress<GetSavesProgress> reporter)
        {
            return new SavesResolverHandler(
                _fs,
                new SceneSerializer(_fs),
                new ScriptListSerializer(_fs),
                _config.VirtAMate.VirtAMateInstallFolder,
                SavesDirectory,
                _config.VirtAMate.IgnoredFolders)
                    .AnalyzeSaves(filter, reporter);
        }

        public async Task<(SavesMap saves, Registry registry)> GetSavesAndRegistryAsync(string[] registries, string filter, IProgress<GetSavesProgress> reporter)
        {
            var isFilterPackage = PackageFullName.TryParsePackage(filter, out var filterPackage);
            var pathFilter = !isFilterPackage && filter != null ? Path.GetFullPath(filter) : null;

            Task<Registry> registryTask;
            Task<SavesMap> savesTask;
            registryTask = GetRegistryAsync(registries);
            // TODO: If the item is a package (no extension), resolve it to a path (if the plugin was not downloaded, throw)
            // TODO: When the filter is a scene, mark every script that was not referenced by that scene as not safe for cleanup; also remove them for display
            savesTask = GetSavesAsync(pathFilter, reporter);

            await Task.WhenAll(registryTask, savesTask).ConfigureAwait(false);

            var registry = await registryTask.ConfigureAwait(false);
            var saves = await savesTask.ConfigureAwait(false);

            if (isFilterPackage)
            {
                var registryPackage = registry.GetPackage(filterPackage);
                if (registryPackage == null)
                    throw new RegistryException($"Could not find package '{registryPackage}'");
                var packageHashes = new HashSet<string>(registryPackage.Versions.SelectMany(v => v.Files).Select(f => f.Hash?.Value).Where(h => h != null).Distinct());
                saves.Scripts = saves.Scripts.Where(s =>
                {
                    if (s is LocalScriptListFile scriptList)
                        return new[] { scriptList.Hash }.Concat(scriptList.Scripts.Select(c => c.Hash)).All(h => packageHashes.Contains(h));
                    else
                        return packageHashes.Contains(s.Hash);
                }).ToArray();
            }

            return (saves, registry);
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

        public IEnumerable<SearchResult> Search(Registry registry, string query)
        {
            return new SearchHandler(_config.Registry.TrustedDomains)
                .Search(registry, query);
        }

        public Task<LocalPackageInfo> GetInstalledPackageInfoAsync(RegistryPackageVersionContext context)
        {
            return new PackageStatusHandler(_fs, _folders)
                .GetInstalledPackageInfoAsync(context);
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

        public Task<(string before, string after)[]> UpdateScriptInSceneAsync(LocalSceneFile scene, LocalScriptFile local, LocalPackageInfo info)
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
        Task<SavesMap> GetSavesAsync(string filter, IProgress<GetSavesProgress> reporter);
        Task<(SavesMap saves, Registry registry)> GetSavesAndRegistryAsync(string[] registries, string filter, IProgress<GetSavesProgress> reporter);
        Task<SortedSet<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string path, DirectoryInfo saves);
        Task<SortedSet<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, Uri url);
        IEnumerable<SearchResult> Search(Registry registry, string query);
        Task<LocalPackageInfo> GetInstalledPackageInfoAsync(RegistryPackageVersionContext context);
        Task<LocalPackageInfo> InstallPackageAsync(LocalPackageInfo info, bool force);
        RegistrySavesMatches MatchSavesToRegistry(SavesMap saves, Registry registry);
        Task<(string before, string after)[]> UpdateScriptInSceneAsync(LocalSceneFile scene, LocalScriptFile local, LocalPackageInfo info);
        Task<string> GetPartyUpdatesAvailable();
        string GetDisplayPath(string path);
        string GetRelativePath(string path, string parentPath);
        void SaveToFile(string data, string path, bool restrict = true);
        void Delete(string path);
        bool Exists(string path);
    }
}
