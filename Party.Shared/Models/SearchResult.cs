using Party.Shared.Models.Registries;

namespace Party.Shared.Models
{
    public class SearchResult
    {
        public bool Trusted { get; set; }
        // TODO: Specify package type and rename other fields
        public RegistryPackage Package { get; set; }
        public Script[] Scripts { get; set; }
        public Scene[] Scenes { get; set; }
    }
}
