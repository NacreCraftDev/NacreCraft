using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//AncientCityStructurePieces structure pool data, maps to vanilla net.minecraft.data.worldgen.AncientCityStructurePieces
public static class AncientCityStructurePieces
{
    public static readonly ResourceKey<RegistryPool> START = Pools.CreateKey("ancient_city/city_center");

    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        var ancientCityStartDegradation = context.LookupOrThrow(Registries.PROCESSOR_LIST).GetOrThrow(ProcessorLists.AncientCityStartDegradation);
        var empty = context.LookupOrThrow(Registries.TEMPLATE_POOL).GetOrThrow(Pools.EMPTY);
        context.Register(START, new GamePool(empty, Pools.Entries(GamePool.Projection.Rigid, (GameElement.Single("ancient_city/city_center/city_center_1", ancientCityStartDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("ancient_city/city_center/city_center_2", ancientCityStartDegradation, GamePool.Projection.Rigid), 1), (GameElement.Single("ancient_city/city_center/city_center_3", ancientCityStartDegradation, GamePool.Projection.Rigid), 1))));
        AncientCityStructurePools.Bootstrap(context);
    }
}
