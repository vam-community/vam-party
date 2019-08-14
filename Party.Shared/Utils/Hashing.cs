using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Party.Shared.Utils
{
    public class Hashing
    {
        public static Task<string> GetHashAsync(IFileSystem fs, string path)
        {
            IEnumerable<string> lines;
            try
            {
                lines = fs.File.ReadAllLines(path);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            return Task.FromResult(GetHash(lines));
        }

        public static string GetHash(IEnumerable<string> lines)
        {
            var content = string.Join('\n', lines);
            var bytes = Encoding.UTF8.GetBytes(content);
            using (var sha256Hash = SHA256.Create())
            {
                byte[] checksum = sha256Hash.ComputeHash(bytes);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}
