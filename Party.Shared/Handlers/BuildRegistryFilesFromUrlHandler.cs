using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Party.Shared.Exceptions;
using Party.Shared.Models.Registries;
using Party.Shared.Utils;

namespace Party.Shared.Handlers
{
    public class BuildRegistryFilesFromUrlHandler
    {
        private readonly HttpClient _http;

        public BuildRegistryFilesFromUrlHandler(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<SortedSet<RegistryFile>> BuildRegistryFilesFromUrlAsync(Registry registry, Uri url)
        {
            if (registry is null) throw new ArgumentNullException(nameof(registry));
            if (url is null) throw new ArgumentNullException(nameof(url));

            return new SortedSet<RegistryFile> { await GetFileFromUrl(url) };
        }

        private async Task<RegistryFile> GetFileFromUrl(Uri url)
        {
            var filename = Path.GetFileName(url.LocalPath);
            if (string.IsNullOrWhiteSpace(filename)) throw new UserInputException($"Url '{url}' does not contain a filename.");
            filename = HttpUtility.UrlDecode(filename);
            if (!filename.EndsWith(".cs")) throw new UserInputException($"Url {url}' does not end with '.cs'");

            using var response = await _http.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var hash = Hashing.GetHash(lines);
            return new RegistryFile
            {
                Filename = filename,
                Url = url.ToString(),
                Hash = new RegistryHash
                {
                    Type = Hashing.Type,
                    Value = hash
                }
            };
        }
    }
}
