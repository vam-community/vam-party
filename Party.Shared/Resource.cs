using System;
using System.IO;
using System.Security.Cryptography;

namespace Party.Shared
{
    public abstract class Resource
    {
        public VamLocation Location { get; }
        protected readonly IScriptHashCache Cache;

        public Resource(VamLocation path, IScriptHashCache cache)
        {
            Location = path;
            Cache = cache;
        }

        public string GetHash()
        {
            return Cache.GetOrCreate(Location.FullPath, _ => GetHashInternal());
        }

        private string GetHashInternal()
        {
            try
            {
                using (var stream = File.OpenRead(Location.FullPath))
                using (var sha256Hash = SHA256.Create())
                {
                    byte[] checksum = sha256Hash.ComputeHash(stream);
                    return BitConverter.ToString(checksum).Replace("-", String.Empty);
                }
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
