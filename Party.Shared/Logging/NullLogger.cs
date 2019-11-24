namespace Party.Shared.Logging
{
    public class NullLogger : ILogger
    {
        public NullLogger()
        {
        }

        public void Log(LogLevel level, string message)
        {
        }
    }
}
