using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Party.Shared.Discovery;
using Party.Shared.Handlers;
using Party.Shared.Registry;
using Party.Shared.Results;

namespace Party.Shared
{
    public class PartyController
    {
        protected PartyConfiguration _config;

        private readonly Lazy<SavesScanner> _scanner;
        private readonly Lazy<SavesResolver> _resolver;
        private readonly Lazy<RegistryClient> _registryClient;

        public PartyController(PartyConfiguration config)
        {
            _config = config;

            _scanner = new Lazy<SavesScanner>(() => new SavesScanner(_config.VirtAMate.SavesDirectory, _config.Scanning.Ignore));
            _resolver = new Lazy<SavesResolver>(() => new SavesResolver());
            _registryClient = new Lazy<RegistryClient>(() => new RegistryClient(_config.Registry.Urls));
        }

        public Task<Registry.Registry> GetRegistryAsync()
        {
            return _registryClient.Value.AcquireAsync();
        }

        public Task<SavesMap> GetSavesAsync()
        {
            return _resolver.Value.Resolve(_scanner.Value.Scan());
        }

        public Task<PublishResult> Publish(string path)
        {
            return new PublishHandler(_config).ExecuteAsync(path);
        }

        public IEnumerable<SearchResult> Search(Registry.Registry registry, SavesMap saves, string query, bool showUsage)
        {
            return new SearchHandler(_config).Execute(registry, saves, query, showUsage);
        }
    }
}
