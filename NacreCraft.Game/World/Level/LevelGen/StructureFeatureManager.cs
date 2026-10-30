using System.Collections.Concurrent;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//StructureFeatureManager is the structure manager, maps to vanilla net.minecraft.world.level.StructureManager
//Holds this world's assembled structure results and cross-chunk references
//One instance is shared per dimension; structures are stored per chunk so the decoration stage can see neighbors' assembled structures
//Chunks advance in parallel during generation, so tables use concurrent dictionaries with lock-free reads, maps to vanilla ConcurrentHashMap
//Previously a single lock covered the whole table; the decoration stage queried the neighborhood structure by structure per chunk, serializing a dozen generation threads
public sealed class StructureFeatureManager
{
    //A chunk may hold assembled results for several structures at once; the key is the packed chunk coordinate
    //The inner map is only written by the thread owning it; concurrent readers are safe
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<Identifier, StructureStart>> _starts = new();
    //A chunk may be covered by several cross-chunk structure bounding boxes, hence a HashSet
    //The set is guarded by itself so appends and snapshots on different chunks stay independent
    private readonly ConcurrentDictionary<long, HashSet<StructureReference>> _references = new();
    //Covered-chunk index: chunk -> starts whose bounding box reaches into it
    //The reference scan and terrain adaptation both asked for "which starts cover this chunk"; without the index every
    //query walked a 17x17 neighbourhood, which turned into O(289 x structures) per chunk
    private readonly ConcurrentDictionary<long, List<StructureStart>> _startsByCoveredChunk = new();

    private readonly ChunkGenerator? _generator;
    private readonly StructurePlacementRegistry? _registry;

    //The parameterless constructor is for scenes without structure generation; CreateStarts then returns 0 directly
    public StructureFeatureManager() { }

    public StructureFeatureManager(ChunkGenerator generator, StructurePlacementRegistry registry)
    {
        _generator = generator;
        _registry = registry;
    }

    //AllStarts is a snapshot of all assembled results, for diagnostics and reference scanning
    //Returns a copy rather than a live view, since other threads keep writing to the table during iteration
    public IEnumerable<StructureStart> AllStarts
    {
        get
        {
            var snapshot = new List<StructureStart>();
            foreach (var perStructure in _starts.Values)
                snapshot.AddRange(perStructure.Values);
            return snapshot;
        }
    }

    //HasStructureReferences queries whether a chunk has structure references
    public bool HasStructureReferences(ChunkPos pos)
    {
        var key = ChunkPos.Pack(pos.X, pos.Z);
        if (!_references.TryGetValue(key, out var set)) return false;
        lock (set) return set.Count > 0;
    }

    //GetReferences returns all structure references of a chunk, or an empty list on miss
    public IReadOnlyList<StructureReference> GetReferences(ChunkPos pos)
    {
        if (!_references.TryGetValue(ChunkPos.Pack(pos.X, pos.Z), out var set))
            return Array.Empty<StructureReference>();
        lock (set) return new List<StructureReference>(set);
    }

    //HasStructureStartsForChunk queries whether a chunk has any assembled structure results
    public bool HasStructureStartsForChunk(ChunkAccess chunk)
        => _starts.TryGetValue(ChunkPos.Pack(chunk.Pos.X, chunk.Pos.Z), out var perStructure)
            && !perStructure.IsEmpty;

    //GetStructureStarts gets all assembled results for the chunk, empty on miss
    public IReadOnlyCollection<StructureStart> GetStructureStarts(ChunkPos pos)
        => _starts.TryGetValue(ChunkPos.Pack(pos.X, pos.Z), out var perStructure)
            ? new List<StructureStart>(perStructure.Values)
            : Array.Empty<StructureStart>();

    //ShouldGenerateStructures says whether this world generates structures, maps to vanilla StructureManager.shouldGenerateStructures
    //Injected from the server config generate-structures; when off, CreateStarts returns 0 and not even structure templates are loaded
    public bool ShouldGenerateStructures { get; set; } = true;

    //SnapshotCovered returns the starts whose bounding box reaches the chunk, an empty array on miss
    //The copy is taken under the list lock, while generation threads keep appending to other chunks
    private StructureStart[] SnapshotCovered(ChunkPos chunk)
    {
        if (!_startsByCoveredChunk.TryGetValue(ChunkPos.Pack(chunk.X, chunk.Z), out var list))
            return Array.Empty<StructureStart>();
        lock (list) return list.ToArray();
    }

    //StartsForStructure gets the given structures whose bounding box covers the target chunk, maps to vanilla StructureManager.startsForStructure
    public IEnumerable<StructureStart> StartsForStructure(ChunkPos chunk, Identifier structureId)
    {
        var target = BoundingBoxInt.FromChunkPos(chunk);
        var result = new List<StructureStart>();
        foreach (var start in SnapshotCovered(chunk))
        {
            if (start.StructureId != structureId) continue;
            if (!start.BoundingBox.Intersects(target)) continue;
            result.Add(start);
        }
        return result;
    }

    //StartsForStructure gets assembled results whose bounding box covers the target chunk and satisfy the predicate, maps to vanilla startsForStructure(pos, predicate)
    //Terrain adaptation filters by terrain_adaptation, only structures that need terrain editing
    public IReadOnlyList<StructureStart> StartsForStructure(ChunkPos chunk, Func<StructureStart, bool> predicate)
    {
        var target = BoundingBoxInt.FromChunkPos(chunk);
        var result = new List<StructureStart>();
        foreach (var start in SnapshotCovered(chunk))
        {
            if (!start.BoundingBox.Intersects(target)) continue;
            if (predicate(start)) result.Add(start);
        }
        return result;
    }

    //IndexStartCoverage records a start under every chunk its bounding box reaches
    //A re-assembly of the same structure from the same source chunk replaces the old entry instead of piling up
    private void IndexStartCoverage(StructureStart start)
    {
        var box = start.BoundingBox;
        var minChunkX = box.MinX >> 4;
        var maxChunkX = box.MaxX >> 4;
        var minChunkZ = box.MinZ >> 4;
        var maxChunkZ = box.MaxZ >> 4;

        for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
        {
            for (var chunkZ = minChunkZ; chunkZ <= maxChunkZ; chunkZ++)
            {
                var list = _startsByCoveredChunk.GetOrAdd(ChunkPos.Pack(chunkX, chunkZ), static _ => new List<StructureStart>());
                lock (list)
                {
                    var replaced = false;
                    for (var index = 0; index < list.Count; index++)
                    {
                        var existing = list[index];
                        if (existing.ChunkPos != start.ChunkPos || existing.StructureId != start.StructureId) continue;
                        list[index] = start;
                        replaced = true;
                        break;
                    }
                    if (!replaced) list.Add(start);
                }
            }
        }
    }

    //AddStructureStart records a structure's assembled result; a repeat assembly of the same structure is overwritten by the latter
    public void AddStructureStart(ChunkPos pos, StructureStart start)
    {
        var key = ChunkPos.Pack(pos.X, pos.Z);
        //The inner dictionary is created and written only by the generation thread owning the chunk; concurrent readers are safe
        var perStructure = _starts.GetOrAdd(key, static _ => new ConcurrentDictionary<Identifier, StructureStart>());
        perStructure[start.StructureId] = start;
        IndexStartCoverage(start);
    }

    //AddStructureReference records a cross-chunk reference; the same structure and source chunk is recorded once
    //Vanilla's reference table is Map<Structure, Set<Long>>; duplicate appends would needlessly bloat persisted data
    public void AddStructureReference(ChunkPos pos, StructureReference reference)
    {
        var key = ChunkPos.Pack(pos.X, pos.Z);
        var set = _references.GetOrAdd(key, static _ => new HashSet<StructureReference>());
        //One set may be appended by several threads; it uses itself as the lock, so different chunks lock independently
        lock (set) set.Add(reference);
    }

    //CreateStarts assembles the structures hit by the current chunk, maps to the set loop in vanilla ChunkGenerator.createStructures
    //Each set picks one structure by weight; a pick whose assembly is invalid is removed and re-rolled until success or no candidates remain
    //Returns the number of successfully assembled structures
    public int CreateStarts(ChunkAccess chunk)
    {
        if (_generator is null || _registry is null) return 0;
        //Skipped entirely when structure generation is off; nothing is assembled and no structure template is loaded
        if (!ShouldGenerateStructures) return 0;
        var sets = _registry.GetSetsForChunk(chunk.Pos);
        var count = 0;
        foreach (var set in sets)
        {
            //Not regenerated when the chunk already has a structure from this set; vanilla decides via hasStructureStart
            if (HasStartForSet(chunk.Pos, set)) continue;
            count += PickAndGenerate(set, chunk);
        }
        return count;
    }

    //PickAndGenerate picks a structure by weight and assembles it, maps to the pick loop in vanilla createStructures
    //The random source and seed derivation must match vanilla exactly, or the structure distribution would differ
    private int PickAndGenerate(StructureSet set, ChunkAccess chunk)
    {
        var options = new List<StructureSelectionEntry>(set.Structures);
        var total = set.WeightTotal;
        var random = new LegacyRandomSource(0L);
        WorldgenRandom.SetLargeFeatureSeed(random, _registry!.Seed, chunk.Pos.X, chunk.Pos.Z);
        while (options.Count > 0)
        {
            var choice = total > 0 ? random.NextInt(total) : 0;
            StructureSelectionEntry? picked = null;
            foreach (var entry in options)
            {
                choice -= entry.Weight;
                if (choice < 0)
                {
                    picked = entry;
                    break;
                }
            }
            if (picked is null) break;
            if (TryGenerate(picked, chunk)) return 1;
            //Invalid assembly; remove the entry and re-roll, decrementing the weight total too
            options.Remove(picked);
            total -= picked.Weight;
        }
        return 0;
    }

    //TryGenerate runs structure assembly and registers the result
    private bool TryGenerate(StructureSelectionEntry entry, ChunkAccess chunk)
    {
        if (!entry.Structure.IsBound() || entry.Structure.Value is not Structure structure) return false;
        var context = new GenerationContext(_generator!, _registry!.Seed, chunk.Pos, chunk);
        //Only structures that declare biomes are filtered; procedural and test structures with no biome declaration are allowed through
        if (structure.Settings.Biomes.Size > 0)
            context.ValidBiome = pos => context.IsBiomeAllowed(structure, pos);
        //Temporary: the stage line cannot say which structure took the time, so a slow assembly is named here
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var loadsBefore = StructureTemplateManager.LoadCount;
        var generated = structure.Generate(context);
        var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        if (elapsed * 1000.0 / System.Diagnostics.Stopwatch.Frequency > 100.0)
            NacreCraft.Logging.Log.Info($"[structure] {structure.Id} assembly={(long)(elapsed * 1000.0 / System.Diagnostics.Stopwatch.Frequency)}ms "
                + $"pieces={generated.Pieces.Count} templates={StructureTemplateManager.LoadCount - loadsBefore} at {chunk.Pos}");
        if (!generated.IsValid) return false;
        AddStructureStart(chunk.Pos, generated);
        return true;
    }

    //HasStartForSet says whether the chunk already has an assembled result for any structure in this set
    private bool HasStartForSet(ChunkPos pos, StructureSet set)
    {
        if (!_starts.TryGetValue(ChunkPos.Pack(pos.X, pos.Z), out var perStructure)) return false;
        foreach (var entry in set.Structures)
        {
            if (!entry.Structure.IsBound()) continue;
            if (entry.Structure.Value is Structure structure && perStructure.ContainsKey(structure.Id))
                return true;
        }
        return false;
    }

    //CollectReferences records every assembled structure whose bounding box covers the target chunk
    //The covered-chunk index already answers that question, so no neighbourhood walk is needed here
    public int CollectReferences(ChunkPos pos)
    {
        var target = BoundingBoxInt.FromChunkPos(pos);
        var bucket = SnapshotCovered(pos);
        var count = 0;
        foreach (var start in bucket)
        {
            if (!start.BoundingBox.Intersects(target)) continue;
            AddStructureReference(pos, new StructureReference(start.StructureId, start.ChunkPos));
            count++;
        }
        return count;
    }
}

//StructureReference is a structure reference recording which structure covers a chunk and the chunk the structure resides in
//record compares by value; the reference table dedupes by it
public sealed record StructureReference(Identifier StructureId, ChunkPos TargetChunk);
