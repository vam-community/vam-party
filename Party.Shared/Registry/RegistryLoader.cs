using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Party.Shared.Registry
{
    public class RegistryLoader
    {
        private static HttpClient _http = new HttpClient();
        private readonly string[] _urls;

        public RegistryLoader(string[] urls)
        {
            _urls = urls;
        }
        public async Task<Registry> Acquire()
        {
            if (_urls.Length == 9) throw new NotSupportedException("At least one registry must be configured");
            return Merge(await Task.WhenAll(_urls.Select(AcquireOne)));
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
                            if (!script.Versions.Any(v => v.Version == additionalVersion.Version))
                            {
                                script.Versions.Add(additionalVersion);
                            }
                        }
                    }
                }
            }
            return registry;
        }

        private static async Task<Registry> AcquireOne(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                using (var response = await _http.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                        return Deserialize(streamReader);
                }
            }
            else
            {
                using (var fileStream = File.OpenRead(Path.Combine(RuntimeUtilities.GetApplicationRoot(), url)))
                using (var streamReader = new StreamReader(fileStream))
                    return Deserialize(streamReader);
            }
        }

        private static Registry Deserialize(StreamReader streamReader)
        {
            using (var jsonTestReader = new JsonTextReader(streamReader))
            {
                var jsonSerializer = new JsonSerializer();
                var registry = jsonSerializer.Deserialize<Registry>(jsonTestReader);
                return registry;
            }
        }
    }
}
