using System.Reflection;
using System.Runtime.Loader;

namespace NacreCraft;

//Kernel assembly loader.
//Through the AssemblyLoadContext.Resolving event, when a sub-library is not found by the runtime,
//load its byte stream from the kernel directory next to the program.
//The embedded resources this used to read were a leftover of the early design; every one of them also ships as a file
//in the kernel directory, so the copy inside the main library was dead weight.
public static class EmbeddedAssemblyLoader
{
    //KernelDirectoryName kernel assembly subdirectory name.
    //Every sub-library is moved there after build, so they stay out of the output root and are resolved on demand.
    public const string KernelDirectoryName = "kernel";

    //Whether already initialized.
    private static int _initialized;

    //Assembly byte rewriter: the mod loader uses it for injection rewrites; when unset, bytes load as-is.
    private static Func<string, byte[], byte[]>? _rewriter;

    //Set the byte rewriter. Takes the assembly name and original bytes, returns the rewritten bytes.
    //Must be set before the target assembly is first resolved; assemblies are resolved on demand, so being late misses the window.
    //Only affects embedded resources and the kernel directory handled by this loader; the main library itself goes through normal resolution and does not pass here, so ordinary mods cannot modify the main library.
    public static void SetRewriter(Func<string, byte[], byte[]>? rewriter)
    {
        _rewriter = rewriter;
    }

    //Register the embedded resource resolution callback. Should be called once at the earliest startup stage.
    //Idempotent: multiple calls take effect only once.
    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        var context = AssemblyLoadContext.Default;
        context.Resolving += OnResolvingAssembly;
    }

    //Callback on resolution failure: load bytes from embedded resources or the kernel directory.
    //Kernel assemblies were moved out of the output root; deps.json still registers them but the file is not found by path,
    //so after default resolution fails it lands here, the bytes pass through the rewriter for the first time, and mod injection happens at this moment.
    private static Assembly? OnResolvingAssembly(AssemblyLoadContext context, AssemblyName name)
    {
        if (string.IsNullOrEmpty(name.Name))
        {
            return null;
        }

        var bytes = ReadAssemblyBytes(name.Name);
        if (bytes is null)
        {
            return null;
        }

        if (_rewriter is not null)
        {
            bytes = _rewriter(name.Name, bytes);
        }

        using var rewritten = new MemoryStream(bytes);
        return context.LoadFromStream(rewritten);
    }

    //ReadAssemblyBytes fetches original bytes by assembly name: the kernel directory first, the run directory as fallback.
    //Returns null when not found. Both the preload-rewrite path and the resolution callback go through here, ensuring both paths get the same source.
    public static byte[]? ReadAssemblyBytes(string assemblyName)
    {
        var kernelPath = Path.Combine(AppPaths.BaseDirectory, KernelDirectoryName, assemblyName + ".dll");
        if (File.Exists(kernelPath))
        {
            return File.ReadAllBytes(kernelPath);
        }

        //Run directory fallback: in output directories that skip the kernel distribution flow, such as tests, the dll still sits in the root
        var directPath = Path.Combine(AppPaths.BaseDirectory, assemblyName + ".dll");
        return File.Exists(directPath) ? File.ReadAllBytes(directPath) : null;
    }

    //ListKernelAssemblies names every assembly sitting in the kernel directory, for diagnostics and mod target collection.
    public static IReadOnlyList<string> ListKernelAssemblies()
    {
        var directory = Path.Combine(AppPaths.BaseDirectory, KernelDirectoryName);
        if (!Directory.Exists(directory))
            return Array.Empty<string>();

        var result = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.dll"))
            result.Add(Path.GetFileNameWithoutExtension(path));
        return result;
    }
}
