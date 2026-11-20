using System.Diagnostics;

namespace NacreCraft.ModLoader;

//LoaderVersionProvider: the version the virtual nc-loader dependency and NcLoader.LoaderVersion report
//Read from the kernel main assembly's version resource; the assembly itself must not be loaded here,
//the loader must not pull the kernel in during bootstrap
internal static class LoaderVersionProvider
{
    private const string KernelAssemblyName = "NacreCraft";
    private const string FallbackVersion = "0.0.0";

    private static readonly Lazy<string> _version = new(ReadVersion);

    public static string Read() => _version.Value;

    private static string ReadVersion()
    {
        //The kernel deploy moves every sub-library into kernel/, the main library included
        var candidates = new[]
        {
            Path.Combine(AppPaths.BaseDirectory, "kernel", KernelAssemblyName + ".dll"),
            Path.Combine(AppPaths.BaseDirectory, KernelAssemblyName + ".dll"),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;

            var version = FileVersionInfo.GetVersionInfo(path).ProductVersion;
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        return FallbackVersion;
    }
}
