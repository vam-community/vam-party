using System;
using System.CommandLine;
using System.IO;
using System.Text.RegularExpressions;

namespace Party.CLI
{
    public class ConsoleRenderer : IConsoleRenderer
    {
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
            return new ColorContext(_resetColor);
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

        public void WriteLine()
        {
            _output.WriteLine();
        }

        public void WriteLine(string text)
        {
            _output.WriteLine(text);
        }

        public void WriteLine(string text, ConsoleColor color)
        {
            _setColor(color);
            _output.WriteLine(text);
            _resetColor();
        }

        public string Ask(string prompt, bool mandatory, Regex regex, string sampleValue)
        {
            string value;
            do
            {
                _output.Write(prompt);
                value = _input.ReadLine();
                value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
            while (!IsValueValid(value, mandatory, regex, sampleValue));
            return value;
        }

        private bool IsValueValid(string value, bool mandatory, Regex regex, string sampleValue)
        {
            if (value == null)
            {
                if (mandatory)
                {
                    WriteLine("Please enter a value.", ConsoleColor.Red);
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

    public interface IConsoleRenderer : IConsole
    {
        IDisposable WithColor(ConsoleColor color);
        void Write(string text);
        void Write(string text, ConsoleColor color);
        void WriteLine();
        void WriteLine(string text);
        void WriteLine(string text, ConsoleColor color);
        string Ask(string prompt, bool mandatory = false, Regex regex = null, string sampleValue = null);
    }
}
