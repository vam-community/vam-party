namespace Party.Shared
{
    public abstract class Resource
    {
        public VamLocation Location { get; }

        public Resource(VamLocation path)
        {
            Location = path;
        }
    }
}
