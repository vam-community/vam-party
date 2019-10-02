namespace Party.Shared.Models.Registries
{
    public class RegistryPackageFileContext : RegistryPackageVersionContext
    {
        public RegistryFile File { get; }

        public RegistryPackageFileContext(RegistryPackageVersionContext context, RegistryFile file)
        : this(context.Registry, context.Package, context.Version, file)
        {
        }

        public RegistryPackageFileContext(Registry registry, RegistryPackage package, RegistryPackageVersion version, RegistryFile file)
        : base(registry, package, version)
        {
            File = file ?? throw new System.ArgumentNullException(nameof(file));
        }

        public void Deconstruct(out Registry registry, out RegistryPackage package, out RegistryPackageVersion version, out RegistryFile file)
        {
            registry = Registry;
            package = Package;
            version = Version;
            file = File;
        }
    }
}
