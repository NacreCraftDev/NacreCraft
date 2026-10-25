using System.Collections.Concurrent;
using NacreCraft.Logging;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Resources;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//StructureTemplateManager structure template manager, maps to the identically named vanilla class
//Reads templates by id from the datapack path data/<ns>/structure/<path>.nbt; every result (including failures) is cached
public sealed class StructureTemplateManager
{
    //Directory template directory, maps to vanilla FileToIdConverter("structure", ".nbt")
    public const string Directory = "structure";

    public const string Extension = ".nbt";

    private readonly ResourceManager _resources;
    //Template cache; generation advances several chunks in parallel, and concurrent writes to a plain dictionary can cycle the bucket chain, manifesting as an insert hanging for tens of seconds
    //Wrapping in Lazy: a template is parsed only once even when requested concurrently, and failures are cached too
    private readonly ConcurrentDictionary<Identifier, Lazy<StructureTemplate?>> _cache = new();

    //_saveDirectory the world's generated directory; structure blocks write <namespace>/structure/<path>.nbt under it
    private string? _saveDirectory;

    public StructureTemplateManager(ResourceManager resources) => _resources = resources;

    //SetSaveDirectory points template saving at the world's generated folder, injected once the world storage is ready
    public void SetSaveDirectory(string? directory) => _saveDirectory = directory;

    //GetOrLoad returns the template for an id, or null when absent; the failure is cached to avoid rescanning resource packs
    public StructureTemplate? GetOrLoad(Identifier id)
        => _cache.GetOrAdd(id,
            static (key, self) => new Lazy<StructureTemplate?>(
                () => self.Load(key), LazyThreadSafetyMode.ExecutionAndPublication),
            this).Value;

    //GetOrCreate returns the cached template, building an empty one in memory when neither the cache nor the packs have it
    //Structure block saving relies on this: the named template may not exist on disk yet
    public StructureTemplate GetOrCreate(Identifier id)
    {
        if (GetOrLoad(id) is { } existing) return existing;
        var created = new StructureTemplate();
        _cache[id] = new Lazy<StructureTemplate?>(
            () => created, LazyThreadSafetyMode.ExecutionAndPublication);
        return created;
    }

    //Save writes a cached template to the generated directory, maps to vanilla StructureTemplateManager.save
    public bool Save(Identifier id)
    {
        if (_saveDirectory is null) return false;
        if (!_cache.TryGetValue(id, out var lazy) || lazy.Value is not { } template) return false;
        var file = Path.Combine(_saveDirectory, id.Namespace, Directory, $"{id.Path}{Extension}");
        try
        {
            var folder = Path.GetDirectoryName(file);
            if (folder is not null) System.IO.Directory.CreateDirectory(folder);
            NbtIo.WriteCompressed(template.Save(new CompoundTag()), file);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Structure template {id} failed to save: {e.Message}");
            return false;
        }
    }

    //Remove drops a template from the cache, maps to vanilla StructureTemplateManager.remove
    public void Remove(Identifier id) => _cache.TryRemove(id, out _);

    //Load builds the template path and reads the NBT
    private StructureTemplate? Load(Identifier id)
    {
        var location = Identifier.FromNamespaceAndPath(id.Namespace, $"{Directory}/{id.Path}{Extension}");
        var resource = _resources.GetResource(PackType.ServerData, location);
        if (resource is null) return null;
        try
        {
            using var stream = resource.Open();
            var tag = NbtIo.ReadCompressed(stream, NbtAccounter.UnlimitedHeap());
            var template = new StructureTemplate();
            template.Load(tag);
            return template;
        }
        catch (Exception e)
        {
            Log.Warning($"Structure template {id} failed to read: {e.Message}");
            return null;
        }
    }
}
