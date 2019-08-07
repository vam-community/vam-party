using System.Collections.Generic;

namespace Party.Shared.Registry
{

    public class Registry
    {
        public List<RegistryScript> Scripts { get; set; }
    }

    public class RegistryScript
    {
        public RegistryScriptAuthor Author { get; set; }
        public string Homepage { get; set; }
        public string Repository { get; set; }
        public List<RegistryScriptVersion> Versions { get; set; }
    }

    public class RegistryScriptAuthor
    {
        public string Name { get; set; }
        public string Profile { get; set; }
    }

    public class RegistryScriptVersion
    {
        public string Version { get; set; }
        public List<RegistryFile> Files { get; set; }
    }

    public class RegistryFile
    {
        public string Filename { get; set; }
        public string Url { get; set; }
        public RegistryFileHash Hash { get; set; }
    }

    public class RegistryFileHash
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
