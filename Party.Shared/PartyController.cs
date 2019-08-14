﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Party.Shared.Handlers;
using Party.Shared.Results;

namespace Party.Shared
{
    public class PartyController
    {
        private static string Version { get; } = typeof(PartyController).Assembly.GetName().Version.ToString();
        private PartyConfiguration _config;
        private HttpClient _http;
        private IFileSystem _fs;

        public PartyController(PartyConfiguration config)
        {
            _config = config;
            _fs = new FileSystem();
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Party", Version));
        }

        public Task<RegistryResult> GetRegistryAsync()
        {
            return new RegistryHandler(_http, _config.Registry.Urls).AcquireAsync();
        }

        public Task<SavesMapResult> GetSavesAsync()
        {
            return new SavesResolverHandler(_fs, _config.VirtAMate.SavesDirectory, _config.Scanning.Ignore).AnalyzeSaves();
        }

        public Task<PublishResult> Publish(string path)
        {
            return new PublishHandler(_config, _fs).PublishAsync(path);
        }

        public IEnumerable<SearchResult> Search(RegistryResult registry, SavesMapResult saves, string query, bool showUsage)
        {
            return new SearchHandler(_config).SearchAsync(registry, saves, query, showUsage);
        }

        public object GetRelativePath(string fullPath)
        {
            if (!fullPath.StartsWith(_config.VirtAMate.SavesDirectory))
            {
                throw new UnauthorizedAccessException($"Only paths within the saves directory are allowed: '{fullPath}'");
            }

            return fullPath.Substring(_config.VirtAMate.SavesDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
        }
    }
}
