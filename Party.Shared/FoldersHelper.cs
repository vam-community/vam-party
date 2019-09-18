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

        public string GetDirectory(RegistryPackageVersionContext context)
        {
            var (_, package, version) = context;
            string author = package.Author ?? "Anonymous";
            // TODO: Packages could define a preferred structure, e.g. for morphs backward compatibility
            var relativeFolder = package.Type switch
            {
                RegistryPackageType.Scripts => _fs.Path.Combine(author, package.Name, version.Version),
                RegistryPackageType.Clothing => _fs.Path.Combine(author, package.Name),
                RegistryPackageType.Scenes => _fs.Path.Combine(author, package.Name),
                RegistryPackageType.Textures => _fs.Path.Combine(author, package.Name),
                RegistryPackageType.Assets => _fs.Path.Combine(author, package.Name),
                RegistryPackageType.FemaleMorphs => _fs.Path.Combine(package.Name),
                RegistryPackageType.MaleMorphs => _fs.Path.Combine(package.Name),
                _ => throw new NotImplementedException($"Type {package.Type} is not currently handled by the folders helper."),
            };
            return _fs.Path.Combine(GetDirectory(package.Type), relativeFolder);
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
                RegistryPackageType.FemaleMorphs => _fs.Path.Combine(_vamDirectory, "Import", "morphs", "female"),
                RegistryPackageType.MaleMorphs => _fs.Path.Combine(_vamDirectory, "Import", "morphs", "male"),
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
    string GetDirectory(RegistryPackageVersionContext context);
    string GetDirectory(RegistryPackageType type);
    string RelativeToVam(string filename);
}
