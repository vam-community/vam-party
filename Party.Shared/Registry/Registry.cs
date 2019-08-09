using System;
using System.Collections.Generic;
using System.Linq;

namespace Party.Shared.Registry
{

    public class Registry
    {
        public List<RegistryScript> Scripts { get; set; }
    }

    public class RegistryScript
    {
        public RegistryScriptAuthor Author { get; set; }
        public string Name { get; set; }
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
        public string Filename { get; set; }
        public string Url { get; set; }
        public RegistryFileHash Hash { get; set; }

        public string GetIdentifier()
        {
            // This should be a common util with Resource
            return $"{Filename}:{Hash.Value ?? "-"}";
        }
    }

    public class RegistryFileHash
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
