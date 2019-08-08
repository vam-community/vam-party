using System;
using System.IO;
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
            // if (_urls.Length != 1) throw new NotSupportedException("Only one registry url is currently supported");
            string url = _urls[0];
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
