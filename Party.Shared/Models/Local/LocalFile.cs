using System.Collections.Generic;
using System.IO;

namespace Party.Shared.Models.Local
{
    public abstract class LocalFile
    {
        public string FullPath { get; }
        public string Hash { get; }
        public string FileName { get => Path.GetFileName(FullPath); }
        public LocalFileErrorLevel Status { get; private set; } = LocalFileErrorLevel.None;
        public List<LocalFileError> Errors { get; private set; }

        protected LocalFile(string fullPath, string hash)
        {
            FullPath = fullPath;
            Hash = hash;
        }

        public void AddError(string message, LocalFileErrorLevel level)
        {
            if (Errors == null) Errors = new List<LocalFileError>();
            if (level > Status) Status = level;
            Errors.Add(new LocalFileError(message, level));
        }

        public override string ToString()
        {
            return $"{{'{FullPath}' -> {Hash ?? "(not hashed)"}}}";
        }
    }
}
