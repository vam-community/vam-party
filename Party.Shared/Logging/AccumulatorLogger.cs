using System;
using System.Collections.Concurrent;

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

        public ILogger For(string name)
        {
            return new LoggerDecorator(this, name);
        }

        public void Log(LogLevel level, string message)
        {
            if (level >= _level)
                _messages.Enqueue(new LogMessage(level, $"{DateTime.Now.ToString("hh:MM:ss.ff")} {message}"));
        }

        public bool Dequeue(out LogMessage message)
        {
            return _messages.TryDequeue(out message);
        }
    }
}
