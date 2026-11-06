using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//PillagerOutpostPools structure pool data, maps to vanilla net.minecraft.data.worldgen.PillagerOutpostPools
public static class PillagerOutpostPools
{
    public static readonly ResourceKey<RegistryPool> START = Pools.CreateKey("pillager_outpost/base_plates");

    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var outpostRot = context.LookupOrThrow(Registries.PROCESSOR_LIST).GetOrThrow(ProcessorLists.OutpostRot);
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(Pools.EMPTY);
        context.Register(START, new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Legacy("pillager_outpost/base_plate", GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "pillager_outpost/towers", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.List(new[] { GameElement.Legacy("pillager_outpost/watchtower", GamePool.Projection.Rigid), GameElement.Legacy("pillager_outpost/watchtower_overgrown", outpostRot, GamePool.Projection.Rigid) }, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "pillager_outpost/feature_plates", new GamePool(empty, Pools.Entries(GamePool.Projection.TerrainMatching, (GameElement.Legacy("pillager_outpost/feature_plate", GamePool.Projection.TerrainMatching), 1))));
        Pools.Register(context, "pillager_outpost/features", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Legacy("pillager_outpost/feature_cage1", GamePool.Projection.Rigid), 1), (GameElement.Legacy("pillager_outpost/feature_cage2", GamePool.Projection.Rigid), 1), (GameElement.Legacy("pillager_outpost/feature_cage_with_allays", GamePool.Projection.Rigid), 1), (GameElement.Legacy("pillager_outpost/feature_logs", GamePool.Projection.Rigid), 1), (GameElement.Legacy("pillager_outpost/feature_tent1", GamePool.Projection.Rigid), 1), (GameElement.Legacy("pillager_outpost/feature_tent2", GamePool.Projection.Rigid), 1), (GameElement.Legacy("pillager_outpost/feature_targets", GamePool.Projection.Rigid), 1), (GameElement.Empty(), 6))));
    }
}
