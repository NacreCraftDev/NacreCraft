using NacreCraft.Registry;
using GamePool = NacreCraft.Game.World.Level.LevelGen.Structure.StructureTemplatePool;
using GameElement = NacreCraft.Game.World.Level.LevelGen.Structure.StructurePoolElement;
using RegistryPool = NacreCraft.Registry.StructureTemplatePool;

namespace NacreCraft.Game.Data.WorldGen;

//VillagePools structure pool data, maps to vanilla net.minecraft.data.worldgen.VillagePools
public static class VillagePools
{
    public static void Bootstrap(BootstrapContext<RegistryPool> context)
    {
        PlainVillagePools.Bootstrap(context);
        SnowyVillagePools.Bootstrap(context);
        SavannaVillagePools.Bootstrap(context);
        DesertVillagePools.Bootstrap(context);
        TaigaVillagePools.Bootstrap(context);
    }
}
