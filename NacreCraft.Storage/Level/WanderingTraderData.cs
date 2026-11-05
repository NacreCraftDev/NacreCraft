using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//WanderingTraderData wandering trader spawn state, maps to vanilla net.minecraft.world.level.saveddata.WanderingTraderData
//Stored in data/minecraft/wandering_trader.dat, one per server; spawn_delay counts down and spawn_chance is the percent chance
public sealed class WanderingTraderData : SavedData
{
    //DefaultSpawnDelay initial countdown before the first attempt, maps to vanilla's 24000
    public const int DefaultSpawnDelay = 24000;

    //DefaultSpawnChance default spawn chance percentage, maps to vanilla's 25
    public const int DefaultSpawnChance = 25;

    //TypeId the save identifier, maps to the minecraft:wandering_trader of the vanilla SavedDataType
    private const string TypeId = "minecraft:wandering_trader";

    //Type, the SavedDataType factory
    public static readonly SavedDataType<WanderingTraderData> Type = new WanderingTraderDataType();

    public override string Id => TypeId;

    //SpawnDelay remaining ticks before the next spawn attempt
    public int SpawnDelay { get; private set; } = DefaultSpawnDelay;

    //SpawnChance percent chance each attempt actually spawns a trader
    public int SpawnChance { get; private set; } = DefaultSpawnChance;

    public void SetSpawnDelay(int spawnDelay)
    {
        if (SpawnDelay == spawnDelay) return;
        SpawnDelay = spawnDelay;
        SetDirty();
    }

    public void SetSpawnChance(int spawnChance)
    {
        if (SpawnChance == spawnChance) return;
        SpawnChance = spawnChance;
        SetDirty();
    }

    //Save field names align with the vanilla WanderingTraderData codec
    public override CompoundTag Save(CompoundTag tag)
    {
        tag.PutInt("spawn_delay", SpawnDelay);
        tag.PutInt("spawn_chance", SpawnChance);
        return tag;
    }

    //WanderingTraderDataType SavedDataType implementation; an empty tag is fresh data, missing fields fall back to defaults
    private sealed class WanderingTraderDataType : SavedDataType<WanderingTraderData>
    {
        public string Id => TypeId;

        public WanderingTraderData Create(CompoundTag tag, RegistryAccess registryAccess)
        {
            var data = new WanderingTraderData();
            if (tag.Count == 0) return data;
            data.SpawnDelay = tag.GetInt("spawn_delay")?.Value ?? DefaultSpawnDelay;
            data.SpawnChance = tag.GetInt("spawn_chance")?.Value ?? DefaultSpawnChance;
            return data;
        }
    }
}
