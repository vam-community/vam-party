using Party.Shared.Models.Registries;

namespace Party.Shared.Models
{
    public class SearchResult
    {
        public bool Trusted { get; set; }
        public RegistryPackage Package { get; set; }
    }
}
