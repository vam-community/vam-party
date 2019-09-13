namespace Party.Shared.Models
{
    public class SavesMap
    {
        public Script[] Scripts { get; set; }
        public Scene[] Scenes { get; set; }
        public SavesError[] Errors { get; set; }
    }

    public class SavesError
    {
        public string File { get; }
        public string Error { get; }
        public SavesErrorLevel Level { get; }

        public SavesError(string file, string error, SavesErrorLevel level)
        {
            File = file;
            Error = error;
            Level = level;
        }
    }

    public enum SavesErrorLevel
    {
        Warning = 1,
        Error = 2
    }
}
