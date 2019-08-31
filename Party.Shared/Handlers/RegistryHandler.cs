using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Exceptions;
using Party.Shared.Models;

namespace Party.Shared.Handlers
{
    public class RegistryHandler
    {
        private readonly HttpClient _http;
        private readonly string[] _urls;

        public RegistryHandler(HttpClient http, string[] urls)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _urls = urls ?? throw new ArgumentNullException(nameof(urls));
        }
        public async Task<Registry> AcquireAsync(string[] registries)
        {
            var urls = registries.Length > 0 ? registries : _urls;
            if (urls.Length == 0)
            {
                throw new ConfigurationException("At least one registry must be configured");
            }
            return Merge(await Task.WhenAll(urls.Select(AcquireOne)).ConfigureAwait(false));
        }

        private Registry Merge(Registry[] registries)
        {
            var registry = registries[0];
            foreach (var additional in registries.Skip(1))
            {
                foreach (var additionalScript in additional.Scripts)
                {
                    var script = registry.Scripts.FirstOrDefault(s => s.Name == additionalScript.Name);
                    if (script == null)
                    {
                        registry.Scripts.Add(additionalScript);
                    }
                    else
                    {
                        foreach (var additionalVersion in additionalScript.Versions)
                        {
                            if (!script.Versions.Any(v => v.Version.Equals(additionalVersion.Version)))
                            {
                                script.Versions.Add(additionalVersion);
                            }
                        }
                    }
                }
            }
            return registry;
        }

        private async Task<Registry> AcquireOne(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                using var response = await _http.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                return Deserialize(streamReader);
            }
            else
            {
                using var fileStream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, url));
                using var streamReader = new StreamReader(fileStream);
                return Deserialize(streamReader);
            }
        }

        private static Registry Deserialize(StreamReader streamReader)
        {
            using var jsonTestReader = new JsonTextReader(streamReader);
            var jsonSerializer = new JsonSerializer();
            var registry = jsonSerializer.Deserialize<Registry>(jsonTestReader);
            return registry;
        }
    }
}
