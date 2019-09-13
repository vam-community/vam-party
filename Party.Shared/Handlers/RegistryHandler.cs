using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Exceptions;
using Party.Shared.Models;
using Party.Shared.Models.Registries;

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
                Merge(registry.Packages.Scripts, additional.Packages.Scripts);
                Merge(registry.Packages.Scenes, additional.Packages.Scenes);
                Merge(registry.Packages.Morphs, additional.Packages.Morphs);
                Merge(registry.Packages.Clothing, additional.Packages.Clothing);
                Merge(registry.Packages.Assets, additional.Packages.Assets);
                Merge(registry.Packages.Textures, additional.Packages.Textures);
            }
            return registry;
        }

        private void Merge(SortedSet<RegistryPackage> registryPackages, SortedSet<RegistryPackage> additionalPackages)
        {
            foreach (var additionalPackage in additionalPackages)
            {
                var script = registryPackages.FirstOrDefault(s => s.Name == additionalPackage.Name);
                if (script == null)
                {
                    registryPackages.Add(additionalPackage);
                }
                else
                {
                    foreach (var additionalVersion in additionalPackage.Versions)
                    {
                        if (!script.Versions.Any(v => v.Version.Equals(additionalVersion.Version)))
                        {
                            script.Versions.Add(additionalVersion);
                        }
                    }
                }
            }
        }

        private async Task<Registry> AcquireOne(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                using var response = await _http.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                return Deserialize(url, streamReader);
            }
            else
            {
                using var fileStream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, url));
                using var streamReader = new StreamReader(fileStream);
                return Deserialize(url, streamReader);
            }
        }

        private Registry Deserialize(string url, StreamReader streamReader)
        {
            using var jsonTestReader = new JsonTextReader(streamReader);
            var jsonSerializer = new JsonSerializer();
            try
            {
                return jsonSerializer.Deserialize<Registry>(jsonTestReader);
            }
            catch (Exception exc)
            {
                throw new RegistryException($"Could not deserialize the registry {url}: {exc.Message}", exc);
            }
        }
    }
}
