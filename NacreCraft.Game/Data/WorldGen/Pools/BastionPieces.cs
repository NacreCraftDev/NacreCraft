using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//BastionPieces structure pool data, maps to vanilla net.minecraft.data.worldgen.BastionPieces
public static class BastionPieces
{
    public static readonly ResourceKey<RegistryPool> START = Pools.CreateKey("bastion/starts");

    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var bastionGenericDegradation = context.LookupOrThrow(Registries.PROCESSOR_LIST).GetOrThrow(ProcessorLists.BastionGenericDegradation);
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(Pools.EMPTY);
        context.Register(START, new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/air_base", bastionGenericDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/hoglin_stable/air_base", bastionGenericDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/treasure/big_air_full", bastionGenericDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/bridge/starting_pieces/entrance_base", bastionGenericDegradation, GamePool.Projection.Rigid), 1))));
        BastionHousingUnitsPools.Bootstrap(context);
        BastionHoglinStablePools.Bootstrap(context);
        BastionTreasureRoomPools.Bootstrap(context);
        BastionBridgePools.Bootstrap(context);
        BastionSharedPools.Bootstrap(context);
    }
}
