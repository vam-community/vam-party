namespace Party.Shared
{
    public abstract class Resource
    {
        public VamLocation Location { get; }
        protected readonly IScriptHashCache Cache;

        public Resource(VamLocation path, IScriptHashCache cache)
        {
            Location = path;
            Cache = cache;
        }
    }
}
