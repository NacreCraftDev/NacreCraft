using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Storage.Chunk;
using NacreCraft.Storage.Paletted;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//ChunkGenerationHelper chunk generation helper
//Wraps ChunkGenerator + ChunkStatusProcessor into the generator callback ServerChunkCache needs
//On a cache miss it drives the ChunkStatus state machine pipeline from EMPTY to FULL to generate a new chunk
public static class ChunkGenerationHelper
{
    //CreateGenerator creates the generator closure and hands the shared structure manager back to the caller for saving and loading
    //minSectionY/sectionsCount build the SimpleLevelHeightAccessor and fix the section range of generated chunks
    //worldSeed drives structure placement; chunkProvider supplies neighbours and, when absent, decoration degrades to the centre chunk only
    public static (Func<ChunkPos, ChunkAccess?> Generator, StructureFeatureManager Structures) CreateGenerator(
        ChunkGenerator generator,
        int minSectionY,
        int sectionsCount,
        RandomSource random,
        PalettedContainerFactory factory,
        long worldSeed = 0L,
        Func<int, int, ChunkAccess?>? chunkProvider = null,
        bool generateStructures = true)
    {
        var level = new SimpleLevelHeightAccessor(minSectionY, sectionsCount);
        //The world seed is injected into the generator; noise random state and structure placement derive from it, decoupled from the caller's random source
        generator.WorldSeed = worldSeed;
        //The base seed is drawn once; each chunk then derives its own random source from its coordinate
        var baseSeed = random.NextLong();
        var structureRegistry = BuildStructureRegistry(worldSeed);
        //Structure results are shared across the dimension; one per chunk would hide neighbouring chunks' assembled structures during decoration
        var structures = new StructureFeatureManager(generator, structureRegistry)
        {
            //With the server generate-structures option off, not even structure templates are loaded
            ShouldGenerateStructures = generateStructures,
        };
        Func<ChunkPos, ChunkAccess?> generatorFunc = pos =>
        {
            //Each chunk gets its own processor and random source; generation runs concurrently on the thread pool
            //A shared instance would corrupt the RandomSource internal state and stall generation
            //Deriving the seed from the coordinate keeps a chunk reproducible
            var processor = new ChunkStatusProcessor(generator, RandomSource.Create(baseSeed + pos.Pack()),
                structureRegistry, worldSeed, chunkProvider, structures);
            return GenerateChunk(processor, level, factory, pos);
        };
        return (generatorFunc, structures);
    }

    //CreateGeneratorAndPipeline builds both paths from one structure state
    //They must share the same StructureFeatureManager or structure data diverges between the two
    public static (Func<ChunkPos, ChunkAccess?> Generator, StructureFeatureManager Structures, IChunkStagePipeline Pipeline)
        CreateGeneratorAndPipeline(
            ChunkGenerator generator,
            int minSectionY,
            int sectionsCount,
            RandomSource random,
            PalettedContainerFactory factory,
            long worldSeed = 0L,
            Func<int, int, ChunkAccess?>? chunkProvider = null,
            bool generateStructures = true)
    {
        var level = new SimpleLevelHeightAccessor(minSectionY, sectionsCount);
        generator.WorldSeed = worldSeed;
        var baseSeed = random.NextLong();
        var structureRegistry = BuildStructureRegistry(worldSeed);
        var structures = new StructureFeatureManager(generator, structureRegistry)
        {
            ShouldGenerateStructures = generateStructures,
        };
        Func<ChunkPos, ChunkAccess?> generatorFunc = pos =>
        {
            var processor = new ChunkStatusProcessor(generator, RandomSource.Create(baseSeed + pos.Pack()),
                structureRegistry, worldSeed, chunkProvider, structures);
            return GenerateChunk(processor, level, factory, pos);
        };
        var pipeline = new StagePipeline(generator, level, factory, baseSeed, worldSeed, chunkProvider,
            structureRegistry, structures);
        return (generatorFunc, structures, pipeline);
    }

    //CreatePipeline builds the stage-driven generator used by ChunkStageScheduler
    //Each chunk keeps its own processor across stages; the scheduler decides when the next stage runs
    public static IChunkStagePipeline CreatePipeline(
        ChunkGenerator generator,
        int minSectionY,
        int sectionsCount,
        RandomSource random,
        PalettedContainerFactory factory,
        long worldSeed = 0L,
        Func<int, int, ChunkAccess?>? chunkProvider = null,
        bool generateStructures = true)
    {
        var level = new SimpleLevelHeightAccessor(minSectionY, sectionsCount);
        generator.WorldSeed = worldSeed;
        var baseSeed = random.NextLong();
        var structureRegistry = BuildStructureRegistry(worldSeed);
        var structures = new StructureFeatureManager(generator, structureRegistry)
        {
            ShouldGenerateStructures = generateStructures,
        };
        return new StagePipeline(generator, level, factory, baseSeed, worldSeed, chunkProvider, structureRegistry, structures);
    }

    //StagePipeline owns the per-chunk processor and exposes one status at a time
    private sealed class StagePipeline : IChunkStagePipeline
    {
        private readonly ChunkGenerator _generator;
        private readonly SimpleLevelHeightAccessor _level;
        private readonly PalettedContainerFactory _factory;
        private readonly long _baseSeed;
        private readonly long _worldSeed;
        private readonly Func<int, int, ChunkAccess?>? _chunkProvider;
        private readonly StructurePlacementRegistry _structureRegistry;
        private readonly StructureFeatureManager _structures;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<long, ChunkStatusProcessor> _processors = new();

        public StagePipeline(ChunkGenerator generator, SimpleLevelHeightAccessor level, PalettedContainerFactory factory,
            long baseSeed, long worldSeed, Func<int, int, ChunkAccess?>? chunkProvider,
            StructurePlacementRegistry structureRegistry, StructureFeatureManager structures)
        {
            _generator = generator;
            _level = level;
            _factory = factory;
            _baseSeed = baseSeed;
            _worldSeed = worldSeed;
            _chunkProvider = chunkProvider;
            _structureRegistry = structureRegistry;
            _structures = structures;
        }

        public ChunkAccess CreateChunk(ChunkPos pos)
        {
            var proto = new ProtoChunk(pos, _level.MinSectionY, _level.SectionsCount,
                _factory.CreateForBlockStates, _factory.CreateForBiomes);
            var processor = new ChunkStatusProcessor(_generator, RandomSource.Create(_baseSeed + pos.Pack()),
                _structureRegistry, _worldSeed, _chunkProvider, _structures);
            _processors[pos.Pack()] = processor;
            return proto;
        }

        public void RunStage(ChunkAccess chunk, ChunkStatus status)
        {
            //Vanilla builds light inside the LIGHT status; doing it here keeps the chunk on its own chain instead of
            //parking it on a separate budgeted thread. The cache serializes the engine with its gate and answers false
            //when the gate is busy, which drops the chunk back onto the light queue
#if NC_LIGHT_IN_STAGE
            if (status == ChunkStatus.LIGHT && _lightInitializer is not null && _lightInitializer(chunk)) return;
#endif
            if (!_processors.TryGetValue(chunk.Pos.Pack(), out var processor))
                throw new InvalidOperationException($"No stage processor for chunk {chunk.Pos}");
            processor.ProcessChunk(chunk, status);
            if (status == ChunkStatus.FULL) _processors.TryRemove(chunk.Pos.Pack(), out _);
        }

        public void SetLightInitializer(Func<ChunkAccess, bool>? initializer) => _lightInitializer = initializer;

        private Func<ChunkAccess, bool>? _lightInitializer;

        public ChunkStatus? NextStatus(ChunkStatus status) => status.Next;

        //CanRunStage holds the surface and decoration stages until the 3x3 around the chunk has biomes
        //Without that gate the surface stage answers every edge lookup with a full climate sample and decoration sees
        //fewer biomes than it should, which is exactly what running ahead of the neighbours costs
        //A neighbour that is not loaded at all does not block: that is the outer ring of the view, and waiting on a chunk
        //that will never exist would strand it
        public bool CanRunStage(ChunkAccess chunk, ChunkStatus status)
        {
            //ChunkStatus holds static instances rather than constants, so this cannot be a switch pattern
            var needsBiomes = status == ChunkStatus.SURFACE || status == ChunkStatus.FEATURES;
            if (!needsBiomes || _chunkProvider is null) return true;
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                if (offsetX == 0 && offsetZ == 0) continue;
                var neighbour = _chunkProvider(chunk.Pos.X + offsetX, chunk.Pos.Z + offsetZ);
                if (neighbour is null) continue;
                if (neighbour.ChunkStatus is not { } neighbourStatus) return false;
                if (!neighbourStatus.IsOrAfter(ChunkStatus.BIOMES)) return false;
            }
            return true;
        }
    }

    //BuildStructureRegistry feeds the loaded structure sets into the placement registry
    //Matches all structure sets held by vanilla ChunkGenerator at construction; missing one means that set's structures never generate
    private static StructurePlacementRegistry BuildStructureRegistry(long worldSeed)
    {
        var registry = new StructurePlacementRegistry(worldSeed);
        foreach (var holder in BuiltInRegistries.STRUCTURE_SET.ListElements())
        {
            //Both the Registry layer and the Game layer have a StructureSet name; the Game layer is the real type carrying placement parameters
            if (holder.Value is NacreCraft.Game.World.Level.LevelGen.Structure.StructureSet set)
                registry.AddSet(set);
        }
        return registry;
    }

    //GenerateChunk single-chunk generation flow, maps to the vanilla chunk generator pipeline
    //1. Build a ProtoChunk at status EMPTY
    //2. Advance to FULL through ChunkStatusProcessor.ProcessToStatus
    //3. Return the proto for ServerChunkCache to cache
    private static ChunkAccess GenerateChunk(
        ChunkStatusProcessor processor,
        LevelHeightAccessor level,
        PalettedContainerFactory factory,
        ChunkPos pos)
    {
        var proto = new ProtoChunk(
            pos,
            level.MinSectionY,
            level.SectionsCount,
            factory.CreateForBlockStates,
            factory.CreateForBiomes);
        processor.ProcessToStatus(proto, ChunkStatus.FULL);
        return proto;
    }
}
