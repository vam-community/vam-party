using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Party.CLI
{
    public interface IRenderer
    {
        void WriteLine(string text);
        Task<string> AskAsync(string prompt);
        Task WhenCompleteAsync();
    }

    public class ConsoleRenderer : IRenderer
    {
        private List<Task> _writing = new List<Task>();
        private readonly TextWriter _output;
        private readonly TextReader _input;

        public ConsoleRenderer(TextWriter output, TextReader input)
        {
            _output = output;
            _input = input;
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

        public async Task<string> AskAsync(string prompt)
        {
            await WhenCompleteAsync().ConfigureAwait(false);
            _output.Write(prompt);
            var value = _input.ReadLine();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
