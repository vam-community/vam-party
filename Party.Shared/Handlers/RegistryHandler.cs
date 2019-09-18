using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Party.Shared.Exceptions;
using Party.Shared.Models.Registries;
using Party.Shared.Serializers;

namespace Party.Shared.Handlers
{
    public class RegistryHandler
    {
        private readonly HttpClient _http;
        private readonly string[] _urls;
        private readonly IRegistrySerializer _serializer;

        public RegistryHandler(HttpClient http, string[] urls, IRegistrySerializer serializer)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _urls = urls ?? throw new ArgumentNullException(nameof(urls));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }
        public async Task<Registry> AcquireAsync(string[] registries)
        {
            var urls = (registries != null && registries.Length > 0) ? registries : _urls;
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
                Merge(registry.Packages, additional.Packages);
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
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return Deserialize(url, stream);
            }
            else
            {
                using var fileStream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, url));
                return Deserialize(url, fileStream);
            }
        }

        private Registry Deserialize(string url, Stream stream)
        {
            try
            {
                return _serializer.Deserialize(stream);
            }
            catch (Exception exc)
            {
                throw new RegistryException($"Could not deserialize the registry {url}: {exc.Message}", exc);
            }
        }
    }
}
