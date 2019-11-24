namespace Party.Shared.Logging
{
    public static class LoggerExtensions
    {
        public static void Verbose(this ILogger @this, string message)
        {
            @this.Log(LogLevel.Verbose, message);
        }

        public static void Info(this ILogger @this, string message)
        {
            @this.Log(LogLevel.Info, message);
        }

        public static void Warning(this ILogger @this, string message)
        {
            @this.Log(LogLevel.Warning, message);
        }

        public static void Error(this ILogger @this, string message)
        {
            @this.Log(LogLevel.Error, message);
        }
    }
}
