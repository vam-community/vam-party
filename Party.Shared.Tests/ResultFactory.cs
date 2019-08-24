using System.Linq;
using Party.Shared.Results;

namespace Party.Shared
{
    public static class ResultFactory
    {
        public static RegistryResult Reg(params RegistryScript[] scripts)
        {
            return new RegistryResult
            {
                Scripts = scripts.ToList()
            };
        }

        public static RegistryScript RegScript(string name, params RegistryScriptVersion[] versions)
        {
            return new RegistryScript
            {
                Name = name,
                Versions = versions.ToList()
            };
        }

        public static RegistryScriptVersion RegVer(string version, params RegistryFile[] files)
        {
            return new RegistryScriptVersion
            {
                Version = version,
                Files = files.ToList()
            };
        }

        public static RegistryFile RegFile(string filename, string hash, string url)
        {
            return new RegistryFile
            {
                Filename = filename,
                Hash = new RegistryFileHash
                {
                    Value = hash
                },
                Url = url
            };
        }
    }
}
