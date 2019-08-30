using Party.Shared.Resources;

namespace Party.Shared.Models
{
    public class RegistrySavesMatch
    {
        public RegistryScript Script { get; set; }
        public RegistryScriptVersion Version { get; set; }
        public RegistryFile File { get; set; }
        public Script Local { get; set; }
    }
}
