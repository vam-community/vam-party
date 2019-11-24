namespace Party.Shared.Logging
{
    public interface ILogger
    {
        ILogger For(string name);
        void Log(LogLevel level, string message);
        bool Dequeue(out LogMessage message);
    }
}
