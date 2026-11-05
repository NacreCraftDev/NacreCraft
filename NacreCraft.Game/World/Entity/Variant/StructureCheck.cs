using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistrySpawnCondition = NacreCraft.Registry.SpawnCondition;

namespace NacreCraft.Game.World.Entity.Variant;

//StructureCheck requires the spawn position to sit inside one of the listed structures, maps to vanilla net.minecraft.world.entity.variant.StructureCheck
//Degraded: the level does not expose a structure manager with a piece-at lookup yet, so no position ever counts as inside
public sealed record StructureCheck(HolderSet<Structure> RequiredStructures) : SpawnCondition
{
    public static readonly MapCodec<StructureCheck> MAP_CODEC = RecordCodecBuilder.Of1(
        HolderSetCodecs.StructureSet.FieldOf("structures").ForGetter((StructureCheck check) => check.RequiredStructures),
        structures => new StructureCheck(structures));

    public static readonly MapCodec<RegistrySpawnCondition> ConditionCodec =
        new XmapMapCodec<StructureCheck, RegistrySpawnCondition>(MAP_CODEC, value => value,
            condition => (StructureCheck)condition);

    public MapCodec<RegistrySpawnCondition> Codec() => ConditionCodec;

    public bool Test(SpawnContext context) => false;
}
