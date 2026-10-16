using NacreCraft.Logging;

namespace NacreCraft.Resources;

//FolderRepositorySource discovers packs in a directory, maps to vanilla net.minecraft.server.packs.repository.FolderRepositorySource
//Only the first level is scanned and a directory needs a pack.mcmeta to count as a pack
//Vanilla also accepts a .zip entry here; NC takes directories only until zip packs are wired up
public sealed class FolderRepositorySource : IRepositorySource
{
    //Discovered packs are optional and insert at the top, maps to vanilla DISCOVERED_PACK_SELECTION_CONFIG
    private static readonly PackSelectionConfig DiscoveredSelectionConfig = new(false, PackPosition.Top, false);

    private readonly string _folder;
    private readonly PackType _packType;
    private readonly PackSource _packSource;

    public FolderRepositorySource(string folder, PackType packType, PackSource packSource)
    {
        _folder = folder;
        _packType = packType;
        _packSource = packSource;
    }

    public void LoadPacks(Action<Pack> consumer)
    {
        try
        {
            Directory.CreateDirectory(_folder);
            foreach (var content in Directory.EnumerateDirectories(_folder))
            {
                if (!File.Exists(Path.Combine(content, "pack.mcmeta")))
                {
                    Log.Info($"Found non-pack entry '{content}', ignoring");
                    continue;
                }
                Accept(content, consumer);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to list packs in {_folder}: {ex.Message}");
        }
    }

    //Accept builds the entry for one pack directory, skipped when its metadata cannot be read
    private void Accept(string content, Action<Pack> consumer)
    {
        var name = Path.GetFileName(content);
        //The file/ prefix keeps a user pack from ever colliding with a built-in id, matching vanilla
        var resources = new FolderPackResources("file/" + name, content);
        var pack = Pack.ReadMetaAndCreate("file/" + name, name, false, resources, _packSource, _packType, DiscoveredSelectionConfig);
        if (pack is null)
        {
            resources.Dispose();
            return;
        }
        consumer(pack);
    }
}
