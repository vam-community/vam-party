using System;
using System.CommandLine;
using System.IO;

namespace Party.CLI
{
    public class SystemConsoleWrapper : IConsole
    {
        public SystemConsoleWrapper(TextWriter consoleOut, TextWriter consoleErr)
        {
            Error = StandardStreamWriter.Create(consoleErr);
            Out = StandardStreamWriter.Create(consoleOut);
        }

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public IStandardStreamWriter Out { get; }

        public bool IsOutputRedirected => Console.IsOutputRedirected;

        public bool IsInputRedirected => Console.IsInputRedirected;
    }
}
