using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level;

//SpawnData the NBT payload of one spawner spawn, maps to vanilla net.minecraft.world.level.SpawnData
//Equipment and the weighted list codec are omitted until the equipment table and codec wiring land
public sealed record SpawnData
{
    public const string EntityTag = "entity";

    public CompoundTag EntityToSpawn { get; }

    //SpawnRules optional light limits, the member is not named CustomSpawnRules because the nested type owns that name
    public CustomSpawnRules? SpawnRules { get; }

    public SpawnData() : this(new CompoundTag(), null)
    {
    }

    public SpawnData(CompoundTag entityToSpawn, CustomSpawnRules? customSpawnRules)
    {
        EntityToSpawn = entityToSpawn;
        SpawnRules = customSpawnRules;
    }

    //CustomSpawnRules light limits a spawned mob must satisfy, maps to vanilla SpawnData.CustomSpawnRules
    public sealed record CustomSpawnRules(InclusiveRange<int> BlockLightLimit, InclusiveRange<int> SkyLightLimit)
    {
        public static readonly InclusiveRange<int> LightRange = new(0, 15);

        //IsValidPosition checks both light limits at the spawn position, maps to vanilla isValidPosition
        //Sky light uses the raw local brightness because ServerLevel has no effective sky brightness query yet
        public bool IsValidPosition(BlockPos blockSpawnPos, ServerLevel level)
            => BlockLightLimit.IsValueInRange(level.GetLightValue(LightLayer.Block, blockSpawnPos))
                && SkyLightLimit.IsValueInRange(level.GetMaxLocalRawBrightness(blockSpawnPos));
    }
}

//Spawner mob spawner contract, maps to vanilla net.minecraft.world.level.Spawner
//The hover text helpers are omitted because they need the typed entity data component pipeline
public interface Spawner
{
    void SetEntityId(EntityType<object>? type, RandomSource random);
}

//CustomSpawner per-level custom spawner tick, maps to vanilla net.minecraft.world.level.CustomSpawner
public interface CustomSpawner
{
    void Tick(ServerLevel level, bool spawnEnemies);
}

//BaseSpawner shared spawner state and delay logic, maps to vanilla net.minecraft.world.level.BaseSpawner
//The server tick body is not decompiled in the reference source and entity creation is not wired, so it is omitted
public abstract class BaseSpawner
{
    public const string SpawnDataTag = "SpawnData";

    private SpawnData? _nextSpawnData;
    private double _spin;
    private double _oSpin;

    public SpawnData? NextSpawnData => _nextSpawnData;

    public WeightedList<SpawnData> SpawnPotentials { get; private set; } = WeightedList<SpawnData>.Of();

    public int SpawnDelay { get; private set; } = 20;

    public int MinSpawnDelay { get; private set; } = 200;

    public int MaxSpawnDelay { get; private set; } = 800;

    public int SpawnCount { get; private set; } = 4;

    public int MaxNearbyEntities { get; private set; } = 6;

    public int RequiredPlayerRange { get; private set; } = 16;

    public int SpawnRange { get; private set; } = 4;

    public double Spin => _spin;

    public double OSpin => _oSpin;

    //BroadcastEvent tells the clients this spawner changed, maps to vanilla broadcastEvent
    public abstract void BroadcastEvent(ServerLevel level, BlockPos pos, int id);

    //SetEntityId stores the entity id of the next spawn data, maps to vanilla setEntityId
    public void SetEntityId(EntityType<object>? type, ServerLevel level, RandomSource random, BlockPos pos)
    {
        if (type is null) return;
        GetOrCreateNextSpawnData(level, random, pos).EntityToSpawn.PutString("id", type.Id.ToString());
    }

    //Delay resets the spawn delay and picks the next spawn data, maps to vanilla delay
    protected void Delay(ServerLevel level, BlockPos pos)
    {
        var random = level.Random;
        SpawnDelay = MaxSpawnDelay <= MinSpawnDelay
            ? MinSpawnDelay
            : MinSpawnDelay + random.NextInt(MaxSpawnDelay - MinSpawnDelay);
        var option = SpawnPotentials.GetRandom(random);
        if (option.IsPresent) SetNextSpawnData(level, pos, option.GetOrThrow());
        BroadcastEvent(level, pos, 1);
    }

    //Load restores the scalar spawner fields; SpawnData and SpawnPotentials need codecs that are not wired yet
    //Maps to vanilla load, degraded for the codec-backed fields
    public void Load(ServerLevel level, BlockPos pos, ValueInput input)
    {
        SpawnDelay = input.GetShortOr("Delay", 20);
        MinSpawnDelay = input.GetIntOr("MinSpawnDelay", 200);
        MaxSpawnDelay = input.GetIntOr("MaxSpawnDelay", 800);
        SpawnCount = input.GetIntOr("SpawnCount", 4);
        MaxNearbyEntities = input.GetIntOr("MaxNearbyEntities", 6);
        RequiredPlayerRange = input.GetIntOr("RequiredPlayerRange", 16);
        SpawnRange = input.GetIntOr("SpawnRange", 4);
    }

    //Save writes the scalar spawner fields; the spawn data payload is skipped for the same codec reason as load
    public void Save(ValueOutput output)
    {
        output.PutShort("Delay", (short)SpawnDelay);
        output.PutShort("MinSpawnDelay", (short)MinSpawnDelay);
        output.PutShort("MaxSpawnDelay", (short)MaxSpawnDelay);
        output.PutShort("SpawnCount", (short)SpawnCount);
        output.PutShort("MaxNearbyEntities", (short)MaxNearbyEntities);
        output.PutShort("RequiredPlayerRange", (short)RequiredPlayerRange);
        output.PutShort("SpawnRange", (short)SpawnRange);
    }

    //OnEventTriggered handles the level event; the server side only needs the delay reset
    //Maps to vanilla onEventTriggered, the client interpolation branch is skipped because this level is server only
    public bool OnEventTriggered(int id)
    {
        if (id != 1) return false;
        SpawnDelay = MinSpawnDelay;
        return true;
    }

    protected void SetNextSpawnData(ServerLevel level, BlockPos pos, SpawnData nextSpawnData)
        => _nextSpawnData = nextSpawnData;

    private SpawnData GetOrCreateNextSpawnData(ServerLevel level, RandomSource random, BlockPos pos)
    {
        if (_nextSpawnData is not null) return _nextSpawnData;
        var option = SpawnPotentials.GetRandom(random);
        SetNextSpawnData(level, pos, option.IsPresent ? option.GetOrThrow() : new SpawnData());
        return _nextSpawnData!;
    }

    //GetOrCreateDisplayEntity and clientTick are omitted: entity creation from NBT and client particles are not wired
    //The spin fields stay so the display rotation can be restored once those land
}
