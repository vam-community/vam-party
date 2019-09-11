namespace System.IO.Abstractions
{
    public static class PathExtensions
    {
#if !NETSTANDARD2_1
        public static string GetFullPath(this IPath @this, string path, string basePath)
        {
            return @this.IsPathRooted(path)
                ? @this.GetFullPath(path)
                : @this.GetFullPath(@this.Combine(basePath, path));
        }
#endif
    }
}
