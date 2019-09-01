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

namespace Party.Shared
{
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
        string GetRelativePath(string fullPath);
        string GetRelativePath(string fullPath, string parentPath);
        void SaveToFile(string data, string path, bool restrict = true);
        void Delete(string fullPath);
    }

    public class PartyController : IPartyController
    {
        private static string Version { get; } = typeof(PartyController).Assembly.GetName().Version.ToString();
        private readonly PartyConfiguration _config;
        private readonly HttpClient _http;
        private readonly IFileSystem _fs;

        public PartyController(PartyConfiguration config)
        {
            _config = config;
            _fs = new FileSystem();
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Party", Version));
        }

        public Task<Registry> GetRegistryAsync(params string[] registries)
        {
            return new RegistryHandler(_http, _config.Registry.Urls).AcquireAsync(registries);
        }

        public Task<SavesMap> GetSavesAsync(string[] filters)
        {
            return new SavesResolverHandler(_fs, _config.VirtAMate.SavesDirectory, _config.Scanning.Ignore).AnalyzeSaves(filters ?? new string[0]);
        }

        public Task<List<RegistryFile>> BuildRegistryFilesFromPathAsync(Registry registry, string name, string path)
        {
            return new RegistryFilesFromPathHandler(_config.VirtAMate.SavesDirectory, _fs).BuildFiles(registry, name, path);
        }

        public Task<List<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, string name, Uri url)
        {
            return new RegistryFilesFromUrlHandler(_http).BuildFiles(registry, name, url);
        }

        public IEnumerable<SearchResult> Search(Registry registry, SavesMap saves, string query)
        {
            return new SearchHandler(_config).Search(registry, saves, query);
        }

        public Task<InstalledPackageInfoResult> GetInstalledPackageInfoAsync(string name, RegistryScriptVersion version)
        {
            return new PackageStatusHandler(_config, _fs).GetInstalledPackageInfoAsync(name, version);
        }

        public Task<InstalledPackageInfoResult> InstallPackageAsync(InstalledPackageInfoResult info)
        {
            return new InstallPackageHandler(_fs, _http).InstallPackageAsync(info);
        }
        public RegistrySavesMatch[] MatchSavesToRegistry(SavesMap saves, Registry registry)
        {
            return new RegistrySavesMatchHandler().Match(saves, registry);
        }

        public Task<(string before, string after)[]> UpdateScriptInSceneAsync(Scene scene, Script local, InstalledPackageInfoResult info)
        {
            return new SceneUpdateHandler(_fs, _config.VirtAMate.SavesDirectory).UpdateScripts(scene, local, info);
        }

        public string GetRelativePath(string fullPath)
        {
            return GetRelativePath(fullPath, _config.VirtAMate.SavesDirectory);
        }

        public string GetRelativePath(string fullPath, string parentPath)
        {
            if (!fullPath.StartsWith(parentPath))
            {
                throw new UnauthorizedAccessException($"Only paths within the saves directory are allowed: '{fullPath}'");
            }

            return fullPath.Substring(parentPath.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        public void SaveToFile(string data, string path, bool restrict)
        {
            if (restrict && !path.StartsWith(_config.VirtAMate.SavesDirectory)) throw new UnauthorizedAccessException($"Cannot save to file {path} because it is not in the Saves folder.");
            _fs.File.WriteAllText(path, data);
        }

        public void Delete(string path)
        {
            if (!path.StartsWith(_config.VirtAMate.SavesDirectory)) throw new UnauthorizedAccessException($"Cannot delete file {path} because it is not in the Saves folder.");
            if (!File.Exists(path))
                return;

            _fs.File.Delete(path);

            while (true)
            {
                path = _fs.Path.GetDirectoryName(path);
                if (path == null) return;
                if (!path.StartsWith(_config.VirtAMate.SavesDirectory)) return;
                if (_fs.Directory.EnumerateFileSystemEntries(path).Any()) return;
                _fs.Directory.Delete(path);
            }
        }
    }
}
