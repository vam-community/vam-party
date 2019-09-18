namespace Party.Shared.Models.Registries
{
    public class RegistryPackageVersionContext
    {
        public Registry Registry { get; }
        public RegistryPackage Package { get; }
        public RegistryPackageVersion Version { get; }

        public RegistryPackageVersionContext(Registry registry, RegistryPackage package, RegistryPackageVersion version)
        {
            Registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            Package = package ?? throw new System.ArgumentNullException(nameof(package));
            Version = version ?? throw new System.ArgumentNullException(nameof(version));
        }


        public RegistryPackageVersionContext WithVersion(RegistryPackageVersion version)
        {
            return Version == version ? this : new RegistryPackageVersionContext(Registry, Package, version);
        }

        public void Deconstruct(out Registry registry, out RegistryPackage package, out RegistryPackageVersion version)
        {
            registry = Registry;
            package = Package;
            version = Version;
        }
    }

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
