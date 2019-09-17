using System;
using System.IO.Abstractions;
using Party.Shared.Models.Registries;

namespace Party.Shared
{
    public class FoldersHelper : IFoldersHelper
    {
        private readonly IFileSystem _fs;
        private readonly string _vamDirectory;

        public FoldersHelper(IFileSystem fs, string vamDirectory)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _vamDirectory = vamDirectory ?? throw new ArgumentNullException(nameof(vamDirectory));
        }

        public string GetDirectory(RegistryPackageType type)
        {
            return type switch
            {
                RegistryPackageType.Scripts => _fs.Path.Combine(_vamDirectory, "Saves", "Scripts"),
                RegistryPackageType.Clothing => _fs.Path.Combine(_vamDirectory, "Custom", "Clothing"),
                RegistryPackageType.Scenes => _fs.Path.Combine(_vamDirectory, "Saves", "scene"),
                RegistryPackageType.Textures => _fs.Path.Combine(_vamDirectory, "Textures"),
                RegistryPackageType.Assets => _fs.Path.Combine(_vamDirectory, "Saves", "Assets"),
                RegistryPackageType.Morphs => _fs.Path.Combine(_vamDirectory, "Import", "morphs"),
                _ => throw new NotImplementedException($"Type {type} is not currently handled by the folders helper."),
            };
        }

        public string RelativeToVam(string filename)
        {
            var result = _fs.Path.GetFullPath(_fs.Path.Combine(_vamDirectory, filename));
            if (!result.StartsWith(_vamDirectory)) throw new UnauthorizedAccessException($"{filename} must traverse paths");
            return result;
        }
    }
}

public interface IFoldersHelper
{
    string GetDirectory(RegistryPackageType type);
    string RelativeToVam(string filename);
}
