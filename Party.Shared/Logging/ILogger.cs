using System.Collections.Generic;

namespace Party.Shared.Logging
{
    public interface ILogger
    {
        void Log(LogLevel level, string message);
        bool Dequeue(out LogMessage message);
    }
}
