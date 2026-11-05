using NacreCraft.Game.World.Level.Block;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//OreVeinifier copper/iron ore vein filler, maps to vanilla net.minecraft.world.level.levelgen.OreVeinifier
//Decides the ore vein block at a position from the toggle/ridged/gap density functions plus a positional random source
//NoiseChunk has no per-block filler hook yet, so Create returns the filler delegate for the caller to invoke
public static class OreVeinifier
{
    private const float VeininessThreshold = 0.4f;
    private const int EdgeRoundoffBegin = 20;
    private const double MaxEdgeRoundoff = 0.2d;
    private const float VeinSolidness = 0.7f;
    private const float MinRichness = 0.1f;
    private const float MaxRichness = 0.3f;
    private const float MaxRichnessThreshold = 0.6f;
    private const float ChanceOfRawOreBlock = 0.02f;
    private const float SkipOreIfGapNoiseIsBelow = -0.3f;

    //BlockStateFiller per-position filler, maps to vanilla NoiseChunk.BlockStateFiller
    //A null result leaves the position untouched
    public delegate BlockState? BlockStateFiller(FunctionContext context);

    public static BlockStateFiller Create(DensityFunction veinToggle, DensityFunction veinRidged, DensityFunction veinGap,
        PositionalRandomFactory oreVeinsPositionalRandomFactory)
    {
        return context =>
        {
            var veininess = veinToggle.Compute(context);
            var posY = context.BlockY;
            var veinType = veininess > 0.0d ? CopperVeinType : IronVeinType;
            var ridged = Math.Abs(veininess);
            var distanceFromTop = veinType.MaxY - posY;
            var distanceFromBottom = posY - veinType.MinY;
            if (distanceFromBottom < 0 || distanceFromTop < 0) return null;
            var distanceFromEdge = Math.Min(distanceFromTop, distanceFromBottom);
            var edgeRoundoff = Mth.ClampedMap(distanceFromEdge, 0.0d, EdgeRoundoffBegin, -MaxEdgeRoundoff, 0.0d);
            if (ridged + edgeRoundoff < VeininessThreshold) return null;
            var positionalRandom = oreVeinsPositionalRandomFactory.At(context.BlockX, posY, context.BlockZ);
            if (positionalRandom.NextFloat() > VeinSolidness) return null;
            if (veinRidged.Compute(context) >= 0.0d) return null;
            var richness = Mth.ClampedMap(ridged, VeininessThreshold, MaxRichnessThreshold, MinRichness, MaxRichness);
            if (positionalRandom.NextFloat() >= richness || veinGap.Compute(context) <= SkipOreIfGapNoiseIsBelow)
                return veinType.Filler;
            return positionalRandom.NextFloat() < ChanceOfRawOreBlock ? veinType.RawOreBlock : veinType.Ore;
        };
    }

    //VeinType ore/raw ore/filler states and the Y band of a vein kind, maps to vanilla OreVeinifier.VeinType
    private sealed record VeinType(BlockState Ore, BlockState RawOreBlock, BlockState Filler, int MinY, int MaxY);

    private static VeinType CopperVeinType => new(Lookup("copper_ore"), Lookup("raw_copper_block"),
        Lookup("granite"), 0, 50);

    private static VeinType IronVeinType => new(Lookup("deepslate_iron_ore"), Lookup("raw_iron_block"),
        Lookup("tuff"), -60, -8);

    //Lookup resolves a block's default state; falls back to air before the block table bootstraps
    private static BlockState Lookup(string path)
        => BuiltInRegistries.BLOCK.GetValue(Identifier.WithDefaultNamespace(path))?.DefaultBlockState
           ?? Blocks.AIR.DefaultBlockState;
}
