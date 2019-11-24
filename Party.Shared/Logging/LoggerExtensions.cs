namespace Party.Shared.Logging
{
    public static class LoggerExtensions
    {
        public static void Verbose(ILogger @this, string message)
        {
            @this.Log(LogLevel.Verbose, message);
        }

        public static void Info(ILogger @this, string message)
        {
            @this.Log(LogLevel.Info, message);
        }

        public static void Warning(ILogger @this, string message)
        {
            @this.Log(LogLevel.Warning, message);
        }

        public static void Error(ILogger @this, string message)
        {
            @this.Log(LogLevel.Error, message);
        }
    }
}
