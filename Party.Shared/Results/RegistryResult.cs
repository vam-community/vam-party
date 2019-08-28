using System.Collections.Generic;
using System.Linq;

namespace Party.Shared.Results
{

    public class Registry
    {
        public List<RegistryScript> Scripts { get; set; }
    }

    public class RegistryScript
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public RegistryScriptAuthor Author { get; set; }
        // TODO: Ensure this only contains valid characters
        public string Homepage { get; set; }
        public string Repository { get; set; }
        public List<RegistryScriptVersion> Versions { get; set; }

        public RegistryScriptVersion GetLatestVersion()
        {
            // TODO: String sorting will not cut it
            return Versions.OrderByDescending(x => x.Version).FirstOrDefault();
        }
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
        // TODO: Ensure this only contains valid characters
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
