using Party.Shared.Registry;
using Party.Shared.Resources;

namespace Party.Shared.Results
{
        public class SearchResult
        {
            public bool Trusted { get; internal set; }
            public RegistryScript Script { get; internal set; }
            public Scene[] Scenes { get; internal set; }
        }
}
