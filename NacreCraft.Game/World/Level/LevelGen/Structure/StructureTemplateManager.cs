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

    //Temporary: number of cold template reads, used to tell template loading from assembly cost
    public static long LoadCount;

    //PrewarmPoolTemplates reads every template the loaded jigsaw pools can place, on a background thread
    //A cold read rebuilds the whole NBT tag tree, 20-700ms for one template, and a single assembly can pull over a hundred
    //of them, which is where the multi-second structure_starts stalls came from; warming moves that cost off the play path
    public void PrewarmPoolTemplates()
    {
        var ids = new HashSet<Identifier>();
        foreach (var holder in BuiltInRegistries.TEMPLATE_POOL.ListElements())
        {
            if (!holder.IsBound() || holder.Value is not StructureTemplatePool pool) continue;
            foreach (var entry in pool.RawTemplates) CollectTemplates(entry.Element, ids);
        }
        if (ids.Count == 0) return;
        var thread = new Thread(() => Warm(ids))
        {
            IsBackground = true,
            Name = "nacrecraft-template-warm",
            Priority = ThreadPriority.BelowNormal,
        };
        thread.Start();
    }

    private static void CollectTemplates(StructurePoolElement element, HashSet<Identifier> ids)
    {
        switch (element)
        {
            case SinglePoolElement single:
                ids.Add(single.TemplateLocation);
                break;
            case ListPoolElement list:
                foreach (var child in list.Elements) CollectTemplates(child, ids);
                break;
        }
    }

    private void Warm(HashSet<Identifier> ids)
    {
        var start = Environment.TickCount64;
        var loaded = 0;
        foreach (var id in ids)
            if (GetOrLoad(id) is not null) loaded++;
        Log.Info($"[template] prewarmed {loaded}/{ids.Count} in {Environment.TickCount64 - start}ms");
    }

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
        //Temporary: cold template reads, reported next to a slow structure assembly
        Interlocked.Increment(ref LoadCount);
        var location = Identifier.FromNamespaceAndPath(id.Namespace, $"{Directory}/{id.Path}{Extension}");
        var resource = _resources.GetResource(PackType.ServerData, location);
        if (resource is null) return null;
        try
        {
            using var stream = resource.Open();
            var readStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var tag = NbtIo.ReadCompressed(stream, NbtAccounter.UnlimitedHeap());
            var readTicks = System.Diagnostics.Stopwatch.GetTimestamp() - readStart;
            var parseStart = System.Diagnostics.Stopwatch.GetTimestamp();
            var template = new StructureTemplate();
            template.Load(tag);
            var parseTicks = System.Diagnostics.Stopwatch.GetTimestamp() - parseStart;
            //Temporary: a cold read that is slow enough to explain an assembly spike is split into NBT read and template conversion
            var toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            var total = (readTicks + parseTicks) * toMs;
            if (total > 20.0)
                Log.Info($"[template] {id} total={(long)total}ms read={(long)(readTicks * toMs)}ms parse={(long)(parseTicks * toMs)}ms");
            return template;
        }
        catch (Exception e)
        {
            Log.Warning($"Structure template {id} failed to read: {e.Message}");
            return null;
        }
    }
}
