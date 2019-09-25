using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Exceptions;
using Party.Shared.Handlers;
using Party.Shared.Models;
using Party.Shared.Models.Local;
using Party.Shared.Models.Registries;
using Party.Shared.Serializers;
using Party.Shared.Utils;

namespace Party.Shared
{
    public class PartyController : IPartyController
    {
        private static string Version { get; } = typeof(PartyController).Assembly.GetName().Version.ToString();
        private readonly bool _checksEnabled;
        private readonly Throttler _throttler = new Throttler();
        private readonly PartyConfiguration _config;
        private readonly HttpClient _http;
        private readonly IFileSystem _fs;
        private readonly IFoldersHelper _folders;

        public PartyController(PartyConfiguration config, bool checksEnabled)
        {
            _config = config;
            _fs = new FileSystem();
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Party", Version));
            _checksEnabled = checksEnabled;
            _folders = new FoldersHelper(_fs, _config.VirtAMate.VirtAMateInstallFolder, _config.VirtAMate.AllowedSubfolders, checksEnabled);
        }

        public void HealthCheck()
        {
            if (!_fs.Directory.Exists(Path.Combine(_config.VirtAMate.VirtAMateInstallFolder, "Saves")))
                throw new SavesException($"Could not find the 'Saves' directory under '{_config.VirtAMate.VirtAMateInstallFolder}'. Either put party.exe in your Virt-A-Mate installation folder, or specify --vam in the options.");
        }

        public Task<Registry> AcquireRegistryAsync(params string[] registries)
        {
            return new AcquireRegistryHandler(_http, _config.Registry.Urls, new RegistrySerializer())
                .AcquireRegistryAsync(registries);
        }

        public Task<SavesMap> ScanLocalFilesAsync(string filter, IProgress<ScanLocalFilesProgress> reporter)
        {
            return new ScanLocalFilesHandler(
                _fs,
                new SceneSerializer(_fs, _throttler),
                new ScriptListSerializer(_fs, _throttler),
                _config.VirtAMate.VirtAMateInstallFolder,
                _config.VirtAMate.AllowedSubfolders,
                _config.VirtAMate.IgnoredFolders)
                    .ScanLocalFilesAsync(filter, reporter);
        }

        public async Task<(SavesMap saves, Registry registry)> ScanLocalFilesAndAcquireRegistryAsync(string[] registries, string filter, IProgress<ScanLocalFilesProgress> reporter)
        {
            var isFilterPackage = PackageFullName.TryParsePackage(filter, out var filterPackage);
            var pathFilter = !isFilterPackage && filter != null ? Path.GetFullPath(filter) : null;
            if (pathFilter != null)
            {
                if (_fs.Directory.Exists(pathFilter))
                    throw new UserInputException("The filter argument cannot be a folder");
                if (!_fs.File.Exists(pathFilter))
                    throw new ArgumentException($"The specified filter '{pathFilter}' is not a valid filename");
            }

            Task<Registry> registryTask;
            Task<SavesMap> savesTask;
            registryTask = AcquireRegistryAsync(registries);
            savesTask = ScanLocalFilesAsync(pathFilter, reporter);

            await Task.WhenAll(registryTask, savesTask).ConfigureAwait(false);

            var registry = await registryTask.ConfigureAwait(false);
            var saves = await savesTask.ConfigureAwait(false);

            if (isFilterPackage)
            {
                var registryPackage = registry.GetPackage(filterPackage);
                if (registryPackage == null)
                    throw new RegistryException($"Could not find package '{filterPackage}'");
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

        public Task<SortedSet<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string path)
        {
            return new BuildRegistryFilesFromPathHandler(_fs)
                .BuildRegistryFilesFromPathAsync(registry, _folders.SanitizePath(path));
        }

        public Task<SortedSet<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, Uri url)
        {
            return new BuildRegistryFilesFromUrlHandler(_http)
                .BuildRegistryFilesFromUrlAsync(registry, url);
        }

        public IEnumerable<SearchResult> FilterRegistry(Registry registry, string query)
        {
            return new FilterRegistryHandler(_config.Registry.TrustedDomains)
                .FilterRegistry(registry, query);
        }

        public Task<LocalPackageInfo> GetInstalledPackageInfoAsync(RegistryPackageVersionContext context)
        {
            return new GetInstalledPackageInfoHandler(_fs, _folders)
                .GetInstalledPackageInfoAsync(context);
        }

        public Task<LocalPackageInfo> InstallPackageAsync(LocalPackageInfo info, bool force)
        {
            return new InstallPackageHandler(_fs, _http)
                .InstallPackageAsync(info, force);
        }

        public RegistrySavesMatches MatchLocalFilesToRegistry(SavesMap saves, Registry registry)
        {
            return new MatchLocalFilesToRegistryHandler()
                .MatchLocalFilesToRegistry(saves, registry);
        }

        public Task<int> UpgradeSceneAsync(LocalSceneFile scene, LocalScriptFile local, LocalPackageInfo info)
        {
            return new UpgradeSceneHandler(new SceneSerializer(_fs, _throttler), _folders)
                .UpgradeSceneAsync(scene, local, info);
        }

        public async Task<string> GetPartyUpdatesAvailableAsync()
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
            return GetRelativePath(path, _config.VirtAMate.VirtAMateInstallFolder);
        }

        public string GetRelativePath(string path, string parentPath)
        {
            path = _folders.SanitizePath(path);
            if (!path.StartsWith(parentPath))
                throw new UnauthorizedAccessException($"Only paths under '{parentPath}' are allowed: '{path}'");

            return path.Substring(parentPath.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        public void SaveRegistry(Registry registry, string path)
        {
            if (_fs.Path.GetFileName(path) != "index.json")
                throw new UserInputException("Looks like the path you provided is not the registry. Make sure the filename is index.json and try again.");

            var serializer = new RegistrySerializer();
            var serialized = serializer.Serialize(registry);

            // NOTE: This is to avoid a hard to reproduce bug where things are out of order sometimes.
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
            var deserialized = serializer.Deserialize(stream);
            serialized = serializer.Serialize(deserialized);

            _fs.File.WriteAllText(path, serialized);
        }

        public void Delete(string path)
        {
            path = _folders.SanitizePath(path);
            if (!_fs.File.Exists(path))
                return;

            _fs.File.Delete(path);

            while (true)
            {
                path = _fs.Path.GetDirectoryName(path);
                if (path == null) return;
                if (!path.StartsWith(_config.VirtAMate.VirtAMateInstallFolder)) return;
                if (_fs.Directory.EnumerateFileSystemEntries(path).Any()) return;
                _fs.Directory.Delete(path);
            }
        }

        public bool Exists(string path)
        {
            return _fs.File.Exists(_folders.SanitizePath(path));
        }
    }

    public interface IPartyController
    {
        void HealthCheck();
        Task<Registry> AcquireRegistryAsync(params string[] registries);
        Task<SavesMap> ScanLocalFilesAsync(string filter, IProgress<ScanLocalFilesProgress> reporter);
        Task<(SavesMap saves, Registry registry)> ScanLocalFilesAndAcquireRegistryAsync(string[] registries, string filter, IProgress<ScanLocalFilesProgress> reporter);
        Task<SortedSet<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string path);
        Task<SortedSet<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, Uri url);
        IEnumerable<SearchResult> FilterRegistry(Registry registry, string query);
        Task<LocalPackageInfo> GetInstalledPackageInfoAsync(RegistryPackageVersionContext context);
        Task<LocalPackageInfo> InstallPackageAsync(LocalPackageInfo info, bool force);
        RegistrySavesMatches MatchLocalFilesToRegistry(SavesMap saves, Registry registry);
        Task<int> UpgradeSceneAsync(LocalSceneFile scene, LocalScriptFile local, LocalPackageInfo info);
        Task<string> GetPartyUpdatesAvailableAsync();
        string GetDisplayPath(string path);
        string GetRelativePath(string path, string parentPath);
        void SaveRegistry(Registry registry, string path);
        void Delete(string path);
        bool Exists(string path);
    }
}
