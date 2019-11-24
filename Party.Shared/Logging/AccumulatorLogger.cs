using System.Collections.Generic;

namespace Party.Shared.Logging
{
    public class AccumulatorLogger : ILogger
    {
        private readonly List<LogMessage> _messages = new List<LogMessage>();
        private readonly LogLevel _level;

        public AccumulatorLogger(LogLevel level)
        {
            _level = level;
        }

        public void Log(LogLevel level, string message)
        {
            if (level >= _level)
                _messages.Add(new LogMessage(level, message));
        }
    }
}
