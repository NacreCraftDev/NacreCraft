using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//BastionHousingUnitsPools structure pool data, maps to vanilla net.minecraft.data.worldgen.BastionHousingUnitsPools
public static class BastionHousingUnitsPools
{
    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var housing = context.LookupOrThrow(Registries.PROCESSOR_LIST).GetOrThrow(ProcessorLists.Housing);
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(Pools.EMPTY);
        Pools.Register(context, "bastion/units/center_pieces", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/center_pieces/center_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/center_pieces/center_1", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/center_pieces/center_2", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/pathways", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/pathways/pathway_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/pathways/pathway_wall_0", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/walls/wall_bases", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/walls/wall_base", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/walls/connected_wall", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/stages/stage_0", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/stages/stage_0_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_0_1", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_0_2", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_0_3", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/stages/stage_1", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/stages/stage_1_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_1_1", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_1_2", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_1_3", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/stages/rot/stage_1", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/stages/rot/stage_1_0", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/stages/stage_2", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/stages/stage_2_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_2_1", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/stages/stage_3", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/stages/stage_3_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_3_1", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_3_2", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/stages/stage_3_3", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/fillers/stage_0", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/fillers/stage_0", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/edges", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/edges/edge_0", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/wall_units", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/wall_units/unit_0", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/edge_wall_units", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/wall_units/edge_0_large", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/ramparts", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/ramparts/ramparts_0", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/ramparts/ramparts_1", housing, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/units/ramparts/ramparts_2", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/large_ramparts", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/ramparts/ramparts_0", housing, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/units/rampart_plates", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/units/rampart_plates/plate_0", housing, GamePool.Projection.Rigid), 1))));
    }
}
