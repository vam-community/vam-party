using System.IO;
using System.Threading.Tasks;

namespace Party.CLI
{
    public interface IRenderer
    {
        Task WriteLineAsync(string text);
    }

    public class ConsoleRenderer : IRenderer
    {
        private readonly TextWriter _output;

        public ConsoleRenderer(TextWriter output)
        {
            _output = output;
        }

        public Task WriteLineAsync(string text)
        {
            return _output.WriteLineAsync(text);
        }
    }
}
