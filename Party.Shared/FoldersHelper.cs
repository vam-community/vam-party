using System;
using System.IO.Abstractions;
using System.Linq;
using Party.Shared.Models.Registries;

namespace Party.Shared
{
    public class FoldersHelper : IFoldersHelper
    {
        private readonly IFileSystem _fs;
        private readonly string _vamDirectory;
        private readonly string[] _allowedSubfolders;
        private readonly bool _checksEnabled;

        public FoldersHelper(IFileSystem fs, string vamDirectory, string[] allowedSubfolders, bool checksEnabled)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _vamDirectory = vamDirectory ?? throw new ArgumentNullException(nameof(vamDirectory));
            _allowedSubfolders = allowedSubfolders ?? new string[0];
            _checksEnabled = checksEnabled;
        }

        public string GetDirectory(RegistryPackageVersionContext context)
        {
            var (_, package, version) = context;
            string author = package.Author ?? "Anonymous";
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
                RegistryPackageType.Scenes => _fs.Path.Combine(_vamDirectory, "Saves", "scene"),
                RegistryPackageType.Scripts => _fs.Path.Combine(_vamDirectory, "Custom", "Scripts"),
                RegistryPackageType.Clothing => _fs.Path.Combine(_vamDirectory, "Custom", "Clothing"),
                RegistryPackageType.Textures => _fs.Path.Combine(_vamDirectory, "Custom", "Atom", "Person", "Textures"),
                RegistryPackageType.Assets => _fs.Path.Combine(_vamDirectory, "Custom", "Assets"),
                RegistryPackageType.FemaleMorphs => _fs.Path.Combine(_vamDirectory, "Custom", "Atom", "Person", "Morphs", "female"),
                RegistryPackageType.MaleMorphs => _fs.Path.Combine(_vamDirectory, "Custom", "Atom", "Person", "Morphs", "male"),
                _ => throw new NotImplementedException($"Type {type} is not currently handled by the folders helper."),
            };
        }

        public string FromRelativeToVam(string filename)
        {
            var result = _fs.Path.GetFullPath(_fs.Path.Combine(_vamDirectory, filename));
            if (!result.StartsWith(_vamDirectory)) throw new UnauthorizedAccessException($"Path traversal is disallowed: '{filename}'");
            return result;
        }

        public string ToRelativeToVam(string path)
        {
            return path.Substring(_vamDirectory.Length).TrimStart(_fs.Path.DirectorySeparatorChar);
        }

        public string SanitizePath(string path)
        {
            path = _fs.Path.GetFullPath(path, _vamDirectory);
            if (_checksEnabled)
            {
                if (!path.StartsWith(_vamDirectory)) throw new UnauthorizedAccessException($"Cannot process path '{path}' because it is not in the Virt-A-Mate installation folder.");
                var localPath = path.Substring(_vamDirectory.Length).TrimStart(new[] { '/', '\\' });
                var directorySeparatorIndex = localPath.IndexOf('\\');
                if (directorySeparatorIndex == -1) throw new UnauthorizedAccessException($"Cannot access files directly at Virt-A-Mate's root");
                var subFolder = localPath.Substring(0, directorySeparatorIndex);
                if (!_allowedSubfolders.Contains(subFolder)) throw new UnauthorizedAccessException($"Accessing Virt-A-Mate subfolder '{subFolder}' is not allowed");
            }
            return path;
        }
    }
}

public interface IFoldersHelper
{
    string GetDirectory(RegistryPackageVersionContext context);
    string GetDirectory(RegistryPackageType type);
    string FromRelativeToVam(string filename);
    string ToRelativeToVam(string path);
    string SanitizePath(string path);
}
