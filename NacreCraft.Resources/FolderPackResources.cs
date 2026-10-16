using NacreCraft.Registry;

namespace NacreCraft.Resources;

//FolderPackResources, a folder-based resource pack, maps to vanilla net.minecraft.server.packs.PathPackResources
//Reads resources from a file system directory, assets/ maps to ClientResources and data/ maps to ServerData
public sealed class FolderPackResources : PackResources
{
    private readonly string _rootPath;
    //_files holds the relative path of every file worth looking up, all using '/', enumerated once at construction
    //Resource lookup is the hottest path during data loading and a File.Exists per call means hitting the disk every time
    //Only the two content roots and the pack root files are indexed; worlds/ and logs/ would only bloat it
    private readonly HashSet<string> _files = new(StringComparer.Ordinal);
    //_namespaces caches the namespace set per type, it follows from _files and never changes for this instance
    private readonly Dictionary<PackType, HashSet<string>> _namespaces = new();

    public FolderPackResources(string packId, string rootPath) : base(packId)
    {
        _rootPath = rootPath;
        ScanFiles();
    }

    //ScanFiles indexes assets/ and data/ recursively plus the files sitting directly in the pack root
    private void ScanFiles()
    {
        foreach (var type in Enum.GetValues<PackType>())
        {
            var typeDir = Path.Combine(_rootPath, TypeToDir(type));
            if (!Directory.Exists(typeDir)) continue;
            foreach (var file in Directory.EnumerateFiles(typeDir, "*", SearchOption.AllDirectories))
                _files.Add(Path.GetRelativePath(_rootPath, file).Replace(Path.DirectorySeparatorChar, '/'));
        }
        if (!Directory.Exists(_rootPath)) return;
        foreach (var file in Directory.EnumerateFiles(_rootPath))
            _files.Add(Path.GetFileName(file));
    }

    //TypeToDir maps a resource type to its root directory name, aligned with the vanilla assets/data convention
    internal static string TypeToDir(PackType type) => type switch
    {
        PackType.ClientResources => "assets",
        PackType.ServerData => "data",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public override Stream? GetRootResource(string path)
    {
        if (!_files.Contains(path)) return null;
        return new FileStream(Path.Combine(_rootPath, path), FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public override Stream? GetResource(PackType type, Identifier location)
    {
        var relativePath = $"{TypeToDir(type)}/{location.Namespace}/{location.Path}";
        if (!_files.Contains(relativePath)) return null;
        return new FileStream(Path.Combine(_rootPath, TypeToDir(type), location.Namespace, location.Path),
            FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public override void ListResources(PackType type, string namespaceName, string pathPrefix, ISet<Identifier> output)
    {
        //The prefix scan answers from the in-memory index instead of walking the directory again
        var prefix = $"{TypeToDir(type)}/{namespaceName}/{pathPrefix}/";
        foreach (var file in _files)
        {
            if (!file.StartsWith(prefix, StringComparison.Ordinal)) continue;
            output.Add(Identifier.FromNamespaceAndPath(namespaceName, $"{pathPrefix}/{file[prefix.Length..]}"));
        }
    }

    public override ISet<string> GetNamespaces(PackType type)
    {
        if (_namespaces.TryGetValue(type, out var cached)) return cached;
        var prefix = TypeToDir(type) + "/";
        var result = new HashSet<string>();
        foreach (var file in _files)
        {
            if (!file.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var end = file.IndexOf('/', prefix.Length);
            if (end > prefix.Length) result.Add(file[prefix.Length..end]);
        }
        _namespaces[type] = result;
        return result;
    }
}
