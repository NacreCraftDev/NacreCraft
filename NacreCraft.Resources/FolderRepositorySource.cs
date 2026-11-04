using NacreCraft.Logging;

namespace NacreCraft.Resources;

//FolderRepositorySource discovers packs in a directory, maps to vanilla net.minecraft.server.packs.repository.FolderRepositorySource
//Only the first level is scanned and a directory needs a pack.mcmeta to count as a pack; the actual classification is PackDetector's job
public sealed class FolderRepositorySource : IRepositorySource
{
    //Discovered packs are optional and insert at the top, maps to vanilla DISCOVERED_PACK_SELECTION_CONFIG
    private static readonly PackSelectionConfig DiscoveredSelectionConfig = new(false, PackPosition.Top, false);

    private readonly string _folder;
    private readonly PackType _packType;
    private readonly PackSource _packSource;
    private readonly FolderPackDetector _detector = new();

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
            foreach (var content in Directory.EnumerateFileSystemEntries(_folder))
            {
                var resources = _detector.DetectPackResources(content);
                if (resources is null)
                {
                    Log.Info($"Found non-pack entry '{content}', ignoring");
                    continue;
                }
                Accept(content, resources, consumer);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to list packs in {_folder}: {ex.Message}");
        }
    }

    //Accept builds the entry for one detected pack, skipped when its metadata cannot be read
    private void Accept(string content, PackResources resources, Action<Pack> consumer)
    {
        var name = Path.GetFileName(content);
        //The file/ prefix keeps a user pack from ever colliding with a built-in id, matching vanilla
        var pack = Pack.ReadMetaAndCreate("file/" + name, name, false, resources, _packSource, _packType, DiscoveredSelectionConfig);
        if (pack is null)
        {
            resources.Dispose();
            return;
        }
        consumer(pack);
    }

    //FolderPackDetector maps a folder entry onto pack resources, maps to vanilla FolderPackDetector
    //Zip packs are not wired up yet, so an archive is reported as a non-pack entry
    private sealed class FolderPackDetector : PackDetector<PackResources>
    {
        protected override PackResources? CreateDirectoryPack(string content)
            => new FolderPackResources("file/" + Path.GetFileName(content), content);

        protected override PackResources? CreateZipPack(string content) => null;
    }
}
