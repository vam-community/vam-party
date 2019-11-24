using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Party.Shared.Logging
{
    public class AccumulatorLogger : ILogger
    {
        private readonly ConcurrentQueue<LogMessage> _messages = new ConcurrentQueue<LogMessage>();
        private readonly LogLevel _level;

        public AccumulatorLogger(LogLevel level)
        {
            _level = level;
        }

        public void Log(LogLevel level, string message)
        {
            if (level >= _level)
                _messages.Enqueue(new LogMessage(level, message));
        }

        public bool Dequeue(out LogMessage message)
        {
            return _messages.TryDequeue(out message);
        }
    }
}
