using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Party.Shared.Resources
{
    public abstract class Resource
    {
        public VamLocation Location { get; }
        protected readonly IHashCache Cache;

        public abstract string Type { get; }

        public Resource(VamLocation path, IHashCache cache)
        {
            Location = path;
            Cache = cache;
        }

        public Task<string> GetHashAsync()
        {
            return Cache.GetOrCreate(Location.FullPath, async _ => await GetHashInternalAsync());
        }

        private async Task<string> GetHashInternalAsync()
        {
            try
            {
                var content = string.Join('\n', await File.ReadAllLinesAsync(Location.FullPath));
                var bytes = Encoding.UTF8.GetBytes(content);
                using (var sha256Hash = SHA256.Create())
                {
                    byte[] checksum = sha256Hash.ComputeHash(bytes);
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

        public string GetIdentifier()
        {
            // TODO: Instead, pre-calculate the hash
            return $"{Location.Filename}!${GetHashAsync().Result}";
        }
    }
}
