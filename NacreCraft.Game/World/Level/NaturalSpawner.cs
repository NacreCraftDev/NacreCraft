using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using RegEntity = NacreCraft.Registry.Entity;
using Mob = NacreCraft.Game.World.Entity.Mob;

namespace NacreCraft.Game.World.Level;

//NaturalSpawner natural mob spawning entry points, maps to vanilla net.minecraft.world.level.NaturalSpawner
//Only the state bookkeeping is ported; the spawn position validation, biome mob settings, structure checks and
//SpawnPlacements pipeline are not available yet, so spawnForChunk / spawnCategoryForPosition are omitted
public static class NaturalSpawner
{
    public const int SpawnDistanceChunk = 8;

    public const int SpawnDistanceBlock = 128;

    public const int InscribedSquareSpawnDistanceChunk = 5;

    private const int MagicNumber = 289;

    //AfterSpawnCallback runs after one mob was placed, maps to vanilla NaturalSpawner.AfterSpawnCallback
    public interface AfterSpawnCallback
    {
        void Run(Mob mob, ChunkAccess levelChunk);
    }

    //ChunkGetter resolves the level chunk of a packed chunk position, maps to vanilla NaturalSpawner.ChunkGetter
    public interface ChunkGetter
    {
        void Query(long chunkKey, Action<LevelChunk> output);
    }

    //SpawnState per-tick spawn bookkeeping, maps to vanilla NaturalSpawner.SpawnState
    public sealed class SpawnState
    {
        private readonly int _spawnableChunkCount;
        private readonly Dictionary<MobCategory, int> _mobCategoryCounts;
        private readonly PotentialCalculator _spawnPotential;
        private readonly LocalMobCapCalculator _localMobCapCalculator;

        public SpawnState(int spawnableChunkCount, Dictionary<MobCategory, int> mobCategoryCounts,
            PotentialCalculator spawnPotential, LocalMobCapCalculator localMobCapCalculator)
        {
            _spawnableChunkCount = spawnableChunkCount;
            _mobCategoryCounts = mobCategoryCounts;
            _spawnPotential = spawnPotential;
            _localMobCapCalculator = localMobCapCalculator;
        }

        public int GetSpawnableChunkCount() => _spawnableChunkCount;

        public IReadOnlyDictionary<MobCategory, int> GetMobCategoryCounts() => _mobCategoryCounts;

        //AddMob records one spawned mob's charge, category count and local player cap usage
        //Maps to vanilla afterSpawn, degraded because the biome spawn cost lookup is not ported
        public void AddMob(BlockPos pos, MobCategory category, double charge)
        {
            _spawnPotential.AddCharge(pos, charge);
            _mobCategoryCounts[category] = _mobCategoryCounts.GetValueOrDefault(category) + 1;
            _localMobCapCalculator.AddMob(new ChunkPos(pos.X >> 4, pos.Z >> 4), category);
        }

        //CanSpawnForCategoryGlobal checks the per-category world-wide cap, maps to vanilla canSpawnForCategoryGlobal
        public bool CanSpawnForCategoryGlobal(MobCategory mobCategory)
        {
            var maxMobCount = MaxInstancesPerChunk(mobCategory) * _spawnableChunkCount / MagicNumber;
            return _mobCategoryCounts.GetValueOrDefault(mobCategory) < maxMobCount;
        }

        public bool CanSpawnForCategoryLocal(MobCategory mobCategory, ChunkPos chunkPos)
            => _localMobCapCalculator.CanSpawn(mobCategory, chunkPos);
    }

    //CreateState counts the loaded mobs and records local cap usage, maps to vanilla createState
    //Degraded: the registry entity type carries no MobCategory, so the caller supplies the category lookup, and the
    //per-biome spawn cost is skipped because biome mob settings are not ported
    public static SpawnState CreateState(int spawnableChunkCount, IEnumerable<RegEntity> entities, ChunkGetter chunkGetter,
        LocalMobCapCalculator localMobCapCalculator, Func<RegEntity, MobCategory?> categoryGetter)
    {
        var spawnPotential = new PotentialCalculator();
        var mobCounts = new Dictionary<MobCategory, int>();
        foreach (var entity in entities)
        {
            var category = categoryGetter(entity);
            if (category is null || category == MobCategory.Misc) continue;
            var pos = new BlockPos((int)Math.Floor(entity.Pos.X), (int)Math.Floor(entity.Pos.Y),
                (int)Math.Floor(entity.Pos.Z));
            chunkGetter.Query(ChunkPos.Pack(pos.X >> 4, pos.Z >> 4), _ =>
            {
                mobCounts[category.Value] = mobCounts.GetValueOrDefault(category.Value) + 1;
                if (entity is Mob)
                    localMobCapCalculator.AddMob(new ChunkPos(pos.X >> 4, pos.Z >> 4), category.Value);
            });
        }
        return new SpawnState(spawnableChunkCount, mobCounts, spawnPotential, localMobCapCalculator);
    }

    //MaxInstancesPerChunk vanilla cap per category, the Registry MobCategory enum carries no such value
    private static int MaxInstancesPerChunk(MobCategory category) => category switch
    {
        MobCategory.Monster => 70,
        MobCategory.Creature => 10,
        MobCategory.Ambient => 15,
        MobCategory.Axolotls => 5,
        MobCategory.UndergroundWaterCreature => 5,
        MobCategory.WaterCreature => 5,
        MobCategory.WaterAmbient => 20,
        _ => -1,
    };
}
