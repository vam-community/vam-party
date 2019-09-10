namespace Party.Shared.Models
{
    public class SearchResult
    {
        public bool Trusted { get; set; }
        public RegistryPackage Package { get; set; }
        public Script[] Scripts { get; set; }
        public Scene[] Scenes { get; set; }
    }
}
