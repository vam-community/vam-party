namespace Party.Shared.Logging
{
    public struct LogMessage
    {
        public LogLevel Level { get; }
        public string Message { get; }

        public LogMessage(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}
