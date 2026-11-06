namespace NacreCraft.Resources;

//VanillaPackResourcesBuilder collects the vanilla pack roots, maps to vanilla net.minecraft.server.packs.VanillaPackResourcesBuilder
//Simplified: NC's VanillaPackResources reads one extracted game root, so the builder produces PathPackResources roots and a
//CompositePackResources when more than one root is pushed; the built-in metadata fallback stays in VanillaPackResources itself
public sealed class VanillaPackResourcesBuilder
{
    private readonly List<string> _rootPaths = new();

    public VanillaPackResourcesBuilder PushUniversalPath(string path) => PushAssetPath(PackType.ClientResources, path)
        .PushAssetPath(PackType.ServerData, path);

    public VanillaPackResourcesBuilder PushAssetPath(PackType packType, string path)
    {
        if (!ValidateDirectory(path)) return this;
        if (!_rootPaths.Contains(path)) _rootPaths.Add(path);
        return this;
    }

    //ExposeNamespace accepted for API parity; NC resolves namespaces by scanning the root
    public VanillaPackResourcesBuilder ExposeNamespace(params string[] namespaces) => this;

    //Build the pack over the collected roots, maps to vanilla build
    public PackResources Build(PackLocationInfo location)
    {
        if (_rootPaths.Count == 0)
            throw new InvalidOperationException("No pack roots were pushed");
        var roots = _rootPaths.Select(path => (PackResources)new PathPackResources(location.Id, path)).ToList();
        if (roots.Count == 1) return roots[0];
        var primary = roots[^1];
        return new CompositePackResources(primary, roots.Take(roots.Count - 1).ToList());
    }

    private static bool ValidateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            if (File.Exists(path)) throw new ArgumentException($"Path {path} is not a directory");
            return false;
        }
        return true;
    }
}
