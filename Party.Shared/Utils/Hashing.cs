using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Party.Shared.Utils
{
    public static class Hashing
    {
        public static string Type => "sha256";

        public static Task<string> GetHashAsync(IFileSystem fs, string path)
        {
            IEnumerable<string> lines;
            try
            {
                lines = fs.File.ReadAllLines(path);
            }
            catch (DirectoryNotFoundException)
            {
                return Task.FromResult<string>(null);
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult<string>(null);
            }
            return Task.FromResult(GetHash(lines));
        }

        public static string GetHash(IEnumerable<string> lines)
        {
            var content = string.Join("\n", lines.Where(l => !string.IsNullOrEmpty(l)));
            var bytes = Encoding.UTF8.GetBytes(content);
            using var sha256Hash = SHA256.Create();
            byte[] checksum = sha256Hash.ComputeHash(bytes);
            return BitConverter.ToString(checksum).Replace("-", string.Empty);
        }
    }
}
