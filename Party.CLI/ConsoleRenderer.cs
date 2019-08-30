using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Party.CLI
{
    public interface IRenderer : IConsole
    {
        IDisposable WithColor(ConsoleColor color);
        void Write(string text);
        void Write(string text, ConsoleColor color);
        void WriteLine(string text);
        Task<string> AskAsync(string prompt, bool mandatory = false, Regex regex = null, string sampleValue = null);
        Task WhenCompleteAsync();
    }

    public class ConsoleRenderer : IRenderer
    {
        private List<Task> _writing = new List<Task>();
        private readonly TextWriter _output;
        private readonly TextReader _input;
        private readonly TextWriter _error;
        private readonly Action<ConsoleColor> _setColor;
        private readonly Action _resetColor;

        public ConsoleRenderer(TextWriter output, TextReader input, TextWriter error, Action<ConsoleColor> setColor, Action resetColor)
        {
            _error = error ?? throw new ArgumentNullException(nameof(error));
            Error = StandardStreamWriter.Create(_error);

            _output = output ?? throw new ArgumentNullException(nameof(output));
            Out = StandardStreamWriter.Create(_output);

            _input = input ?? throw new ArgumentNullException(nameof(input));

            _setColor = setColor ?? throw new ArgumentNullException(nameof(setColor));
            _resetColor = resetColor ?? throw new ArgumentNullException(nameof(resetColor));
        }

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public IStandardStreamWriter Out { get; }

        public bool IsOutputRedirected => Console.IsOutputRedirected;

        public bool IsInputRedirected => Console.IsInputRedirected;

        public IDisposable WithColor(ConsoleColor color)
        {
            _setColor(color);
            return new ColorContext(() => { _resetColor(); WhenCompleteAsync().ConfigureAwait(false).GetAwaiter().GetResult(); });
        }

        public void Write(string text)
        {
            _output.Write(text);
        }

        public void Write(string text, ConsoleColor color)
        {
            _setColor(color);
            _output.Write(text);
            _resetColor();
        }

        public void WriteLine(string text)
        {
            _writing.Add(_output.WriteLineAsync(text));
        }

        public async Task WhenCompleteAsync()
        {
            var tasks = _writing;
            _writing = new List<Task>();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task<string> AskAsync(string prompt, bool mandatory = true, Regex regex = null, string sampleValue = null)
        {
            await WhenCompleteAsync().ConfigureAwait(false);
            string value;
            do
            {
                _output.Write(prompt);
                value = _input.ReadLine();
                value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            } while (!IsValueValid(value, mandatory, regex, sampleValue));
            return value;
        }

        private bool IsValueValid(string value, bool mandatory, Regex regex, string sampleValue)
        {
            if (value == null)
            {
                if (mandatory)
                {
                    using (WithColor(ConsoleColor.Red))
                    {
                        _output.WriteLine("Please enter a value.");
                    }
                    return false;
                }
                return true;

            }

            if (regex == null)
                return true;

            if (!regex.IsMatch(value))
            {
                using (WithColor(ConsoleColor.Red))
                {
                    _output.WriteLine("This value is invalid.");
                    if (sampleValue != null)
                        _output.WriteLine($"Example: '{sampleValue}'");
                }
                return false;
            }

            return true;
        }

        private class ColorContext : IDisposable
        {
            private readonly Action _resetColor;

            public ColorContext(Action resetColor)
            {
                _resetColor = resetColor;
            }

            public void Dispose()
            {
                _resetColor();
            }
        }
    }
}
