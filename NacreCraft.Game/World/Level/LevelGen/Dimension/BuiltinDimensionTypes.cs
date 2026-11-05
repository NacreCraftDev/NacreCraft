using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen.Dimension;

//BuiltinDimensionTypes registry keys of the built-in dimension types, maps to vanilla net.minecraft.world.level.dimension.BuiltinDimensionTypes
//The keys are data driven; the dimension type elements themselves are loaded from the datapack
public static class BuiltinDimensionTypes
{
    public static readonly ResourceKey<NacreCraft.Registry.DimensionType> Overworld = Register("overworld");
    public static readonly ResourceKey<NacreCraft.Registry.DimensionType> Nether = Register("the_nether");
    public static readonly ResourceKey<NacreCraft.Registry.DimensionType> End = Register("the_end");
    public static readonly ResourceKey<NacreCraft.Registry.DimensionType> OverworldCaves = Register("overworld_caves");

    private static ResourceKey<NacreCraft.Registry.DimensionType> Register(string id)
        => ResourceKey<NacreCraft.Registry.DimensionType>.Create(Registries.DIMENSION_TYPE,
            Identifier.WithDefaultNamespace(id));
}
