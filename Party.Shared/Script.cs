using System;
using System.IO;
using System.Security.Cryptography;

namespace Party.Shared
{
    public class Script : Resource
    {
        public Script(string file)
        : base(file)
        {
        }

        public string GetHash()
        {
            using (var stream = File.OpenRead(FullPath))
            using (var sha256Hash = SHA256.Create())
            {
                byte[] checksum = sha256Hash.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}
