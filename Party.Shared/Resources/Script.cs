namespace Party.Shared.Resources
{
    public class Script : Resource
    {
        public override string Type { get => "cs"; }

        public Script(VamLocation path, IHashCache cache)
        : base(path, cache)
        {
        }
    }
}
