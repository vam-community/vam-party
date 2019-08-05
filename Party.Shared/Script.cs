using System;
using System.IO;
using System.Security.Cryptography;

namespace Party.Shared
{
    public class Script : Resource
    {
        public Script(VamLocation path)
        : base(path)
        {
        }

        public string GetHash()
        {
            using (var stream = File.OpenRead(Location.FullPath))
            using (var sha256Hash = SHA256.Create())
            {
                byte[] checksum = sha256Hash.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}
