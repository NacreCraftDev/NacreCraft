using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level;

//FluidTags built-in fluid tag keys, maps to vanilla net.minecraft.tags.FluidTags
//FluidState does not carry a tag lookup yet, so consumers match Water/Lava by identity of these keys
public static class FluidTags
{
    public static readonly TagKey<Fluid> Water = TagKey<Fluid>.Create(Registries.FLUID, Identifier.WithDefaultNamespace("water"));
    public static readonly TagKey<Fluid> Lava = TagKey<Fluid>.Create(Registries.FLUID, Identifier.WithDefaultNamespace("lava"));
}
