using Party.Shared.Resources;

namespace Party.Shared.Results
{
    public class SearchResult
    {
        public bool Trusted { get; internal set; }
        public RegistryResult.RegistryScript Package { get; internal set; }
        public Script[] Scripts { get; internal set; }
        public Scene[] Scenes { get; internal set; }
    }
}
