using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//BastionSharedPools structure pool data, maps to vanilla net.minecraft.data.worldgen.BastionSharedPools
public static class BastionSharedPools
{
    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(Pools.EMPTY);
        Pools.Register(context, "bastion/mobs/piglin", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/mobs/melee_piglin", GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/mobs/sword_piglin", GamePool.Projection.Rigid), 4), (GameElement.Single("bastion/mobs/crossbow_piglin", GamePool.Projection.Rigid), 4), (GameElement.Single("bastion/mobs/empty", GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/mobs/hoglin", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/mobs/hoglin", GamePool.Projection.Rigid), 2), (GameElement.Single("bastion/mobs/empty", GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/blocks/gold", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/blocks/air", GamePool.Projection.Rigid), 3), (GameElement.Single("bastion/blocks/gold", GamePool.Projection.Rigid), 1))));
        Pools.Register(context, "bastion/mobs/piglin_melee", new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("bastion/mobs/melee_piglin_always", GamePool.Projection.Rigid), 1), (GameElement.Single("bastion/mobs/melee_piglin", GamePool.Projection.Rigid), 5), (GameElement.Single("bastion/mobs/sword_piglin", GamePool.Projection.Rigid), 1))));
    }
}
