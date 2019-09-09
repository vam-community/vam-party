using System.Collections.Generic;
using System.IO;

namespace Party.Shared.Models
{
    public abstract class Resource
    {
        public string FullPath { get; }
        public string Hash { get; }
        public string FileName { get => Path.GetFileName(FullPath); }

        protected Resource(string fullPath, string hash)
        {
            FullPath = fullPath;
            Hash = hash;
        }
    }

    public class Scene : Resource
    {
        public List<Script> Scripts { get; } = new List<Script>();

        public Scene(string fullPath)
            : base(fullPath, null)
        {
        }

        internal void References(Script script)
        {
            Scripts.Add(script);
        }
    }

    public class Script : Resource
    {
        public List<Scene> Scenes { get; } = new List<Scene>();

        public Script(string fullPath, string hash)
        : base(fullPath, hash)
        {
        }

        internal void ReferencedBy(Scene scene)
        {
            if (!Scenes.Contains(scene))
            {
                Scenes.Add(scene);
            }
        }
    }

    public class ScriptList : Script
    {
        public Script[] Scripts { get; }

        public ScriptList(string fullPath, string hash, Script[] scripts)
        : base(fullPath, hash)
        {
            Scripts = scripts;
        }
    }
}
