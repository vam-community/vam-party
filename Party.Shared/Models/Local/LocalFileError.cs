namespace Party.Shared.Models.Local
{
    public class LocalFileError
    {
        public string Error { get; }
        public LocalFileErrorLevel Level { get; }

        public LocalFileError(string error, LocalFileErrorLevel level)
        {
            Error = error;
            Level = level;
        }
    }
}
