using Party.Shared.Resources;

namespace Party.Shared.Results
{
    public class SearchResult
    {
        public bool Trusted { get; set; }
        public RegistryScript Package { get; set; }
        public Script[] Scripts { get; set; }
        public Scene[] Scenes { get; set; }
    }
}
