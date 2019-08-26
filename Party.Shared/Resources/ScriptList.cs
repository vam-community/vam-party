using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Party.Shared.Resources
{
    public class ScriptList : Script
    {
        private Script[] Scripts { get; }

        public ScriptList(string fullPath, string hash, Script[] scripts)
        : base(fullPath, hash)
        {
            Scripts = scripts;
        }
    }
}
