using NacreCraft.Logging;

namespace NacreCraft.Resources;

//Pack a resource pack the repository can offer, maps to vanilla net.minecraft.server.packs.repository.Pack
//Holds the metadata read out of pack.mcmeta plus the content accessor
//Vanilla defers opening through a ResourcesSupplier; NC opens once at discovery and reuses the instance across reloads
public sealed class Pack
{
    //Id repository key, vanilla uses "vanilla" for the built-in pack and "file/<folder>" for a discovered one
    public string Id { get; }

    //Title display name, taken from the location info rather than pack.mcmeta
    public string Title { get; }

    //Description the pack.description value out of pack.mcmeta
    public string Description { get; }

    //IsBuiltin built-in packs are not removable from the repository
    public bool IsBuiltin { get; }

    public PackResources Resources { get; }

    public PackSelectionConfig SelectionConfig { get; }

    public PackSource Source { get; }

    public PackCompatibility Compatibility { get; }

    //IsRequired a required pack cannot be deselected, which is how the vanilla pack stays on
    public bool IsRequired => SelectionConfig.Required;

    //IsFixedPosition a fixed pack cannot be moved around the selection list
    public bool IsFixedPosition => SelectionConfig.FixedPosition;

    //DefaultPosition the end a required pack inserts at
    public PackPosition DefaultPosition => SelectionConfig.DefaultPosition;

    public Pack(string id, string title, string description, bool isBuiltin, PackResources resources,
        PackSelectionConfig selectionConfig, PackSource source, PackCompatibility compatibility)
    {
        Id = id;
        Title = title;
        Description = description;
        IsBuiltin = isBuiltin;
        Resources = resources;
        SelectionConfig = selectionConfig;
        Source = source;
        Compatibility = compatibility;
    }

    //ReadMetaAndCreate reads pack.mcmeta and builds a pack, null when its metadata cannot be read
    //maps to vanilla Pack.readMetaAndCreate; the caller skips a pack that comes back null rather than treating it as broken
    public static Pack? ReadMetaAndCreate(string id, string title, bool isBuiltin, PackResources resources,
        PackSource source, PackType packType, PackSelectionConfig selectionConfig)
    {
        var current = PackCompatibilityRules.GameFormat(packType);
        var meta = ReadMetadata(resources, packType, current);
        if (meta is null) return null;
        return new Pack(id, title, meta.Value.Description, isBuiltin, resources, selectionConfig, source, meta.Value.Compatibility);
    }

    //ReadMetadata reads the pack section and classifies it against the running format
    //maps to vanilla Pack.readPackMetadata: a parse failure retries the fallback section, and null means the pack is skipped
    private static (string Description, PackCompatibility Compatibility)? ReadMetadata(PackResources resources, PackType packType, PackFormat current)
    {
        PackMetadataSection? meta;
        try
        {
            meta = resources.GetMetadataSection(PackMetadataSection.ForPackType(packType));
        }
        catch (Exception ex)
        {
            Log.Warning($"Error reading pack metadata for {resources.PackId}, attempting fallback type: {ex.Message}");
            meta = resources.GetMetadataSection(PackMetadataSection.FallbackType);
        }
        if (meta is null)
        {
            Log.Warning($"Missing metadata in pack {resources.PackId}");
            return null;
        }
        return (meta.Description, PackCompatibilityRules.ForVersion(meta.SupportedFormats, current));
    }

    //Open hands back the content accessor, maps to vanilla Pack.open
    //NC keeps the accessor alive instead of reopening the pack on every reload
    public PackResources Open() => Resources;

    public override string ToString() => $"Pack[{Id} {Compatibility}]";
}
