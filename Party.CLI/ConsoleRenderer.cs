using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Party.CLI
{
    public interface IRenderer
    {
        void WriteLine(string text);
        Task WhenComplete();
    }

    public class ConsoleRenderer : IRenderer
    {
        private List<Task> _writing = new List<Task>();
        private readonly TextWriter _output;

        public ConsoleRenderer(TextWriter output)
        {
            _output = output;
        }

        public void WriteLine(string text)
        {
            _writing.Add(_output.WriteLineAsync(text));
        }

        public async Task WhenComplete()
        {
            var tasks = _writing;
            _writing = new List<Task>();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
