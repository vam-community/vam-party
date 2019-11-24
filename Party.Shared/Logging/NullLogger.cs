namespace Party.Shared.Logging
{
    public class NullLogger : ILogger
    {
        public NullLogger()
        {
        }

        public ILogger For(string name)
        {
            return this;
        }

        public void Log(LogLevel level, string message)
        {
        }

        public bool Dequeue(out LogMessage message)
        {
            message = default;
            return false;
        }
    }
}
