using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//BastionBridgePools structure pool data, maps to vanilla net.minecraft.data.worldgen.BastionBridgePools
public static class BastionBridgePools
{
    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var holderGetterLookup = context.LookupOrThrow(Registries.PROCESSOR_LIST);
        var entranceReplacement = holderGetterLookup.GetOrThrow(ProcessorLists.EntranceReplacement);
        var bastionGenericDegradation = holderGetterLookup.GetOrThrow(ProcessorLists.BastionGenericDegradation);
        var bridge = holderGetterLookup.GetOrThrow(ProcessorLists.Bridge);
        var rampartDegradation = holderGetterLookup.GetOrThrow(ProcessorLists.RampartDegradation);
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(Pools.EMPTY);
        Pools.Register(context, "bastion/bridge/starting_pieces", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/starting_pieces/entrance", entranceReplacement, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/bridge/starting_pieces/entrance_face", bastionGenericDegradation, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/bridge/bridge_pieces", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/bridge_pieces/bridge", bridge, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/bridge/legs", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/legs/leg_0", bastionGenericDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/bridge/legs/leg_1", bastionGenericDegradation, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/bridge/walls", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/walls/wall_base_0", rampartDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/bridge/walls/wall_base_1", rampartDegradation, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/bridge/ramparts", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/ramparts/rampart_0", rampartDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/bridge/ramparts/rampart_1", rampartDegradation, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/bridge/rampart_plates", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/rampart_plates/plate_0", rampartDegradation, GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/bridge/connectors", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/bridge/connectors/back_bridge_top", bastionGenericDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/bridge/connectors/back_bridge_bottom", bastionGenericDegradation, GamePool.Projection.Rigid), 1))));
    }
}
