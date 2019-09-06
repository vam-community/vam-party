using Moq;
using NUnit.Framework;
using Party.Shared;
using System;
using System.CommandLine;
using System.IO;
using System.Text;

namespace Party.CLI
{
    public abstract class CommandTestsBase : IDisposable
    {
        protected Mock<IConsoleRenderer> _renderer;
        protected Mock<IPartyController> _controller;
        protected Program _program;
        protected StringBuilder _out;
        private StringWriter _stringWriter;

        [SetUp]
        public void CreateDependencies()
        {
            _out = new StringBuilder();
            _stringWriter = new StringWriter(_out);
            var outWriter = StandardStreamWriter.Create(_stringWriter);
            _renderer = new Mock<IConsoleRenderer>(MockBehavior.Strict);
            _renderer.Setup(x => x.WriteLine()).Callback(() => _out.Append($"\n"));
            _renderer.Setup(x => x.WriteLine(It.IsAny<string>())).Callback((string line) => _out.Append($"{line}\n"));
            _renderer.Setup(x => x.WriteLine(It.IsAny<string>(), It.IsAny<ConsoleColor>())).Callback((string line, ConsoleColor color) => _out.Append($"[color:{color}]{line}[/color]\n"));
            _renderer.Setup(x => x.Write(It.IsAny<string>())).Callback((string text) => _out.Append(text));
            _renderer.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<ConsoleColor>())).Callback((string text, ConsoleColor color) => _out.Append($"[color:{color}]{text}[/color]"));
            _renderer.Setup(x => x.WithColor(It.IsAny<ConsoleColor>())).Returns((ConsoleColor color) => new ColorStub(_out, color));
            _renderer.Setup(x => x.Out).Returns(outWriter);
            _renderer.Setup(x => x.Error).Returns(outWriter);
            _controller = new Mock<IPartyController>(MockBehavior.Strict);
            _controller.SetupProperty(mock => mock.ChecksEnabled);
            var config = PartyConfigurationFactory.Create(@"C:\VaM");
            _program = new Program(_renderer.Object, config, _controller.Object);
        }

        protected string[] GetOutput()
        {
            var output = _out.ToString().Trim();
            if (output.Contains("Exception: "))
            {
                Assert.Fail($"Exception detected:{Environment.NewLine}{output}");
            }
            return output.Split(new[] { '\r', '\n' });
        }

        private class ColorStub : IDisposable
        {
            private readonly StringBuilder _out;

            public ColorStub(StringBuilder output, ConsoleColor color)
            {
                _out = output;
                _out.Append($"[color:{color}]");
            }

            public void Dispose()
            {
                _out.Append("[/color]");
            }
        }

        public void Dispose()
        {
            _stringWriter?.Dispose();
        }
    }
}
