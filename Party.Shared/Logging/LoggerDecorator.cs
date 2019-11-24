using System;

namespace Party.Shared.Logging
{
    public class LoggerDecorator : ILogger
    {
        private readonly ILogger _logger;
        private readonly string _name;

        public LoggerDecorator(ILogger logger, string name)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public bool Dequeue(out LogMessage message)
        {
            throw new NotSupportedException();
        }

        public ILogger For(string name)
        {
            return new LoggerDecorator(this, name);
        }

        public void Log(LogLevel level, string message)
        {
            _logger.Log(level, $"[{_name}] {message}");
        }
    }
}
