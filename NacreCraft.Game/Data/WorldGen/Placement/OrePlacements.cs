using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Registry;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//OrePlacements built-in placed ore veins, maps to vanilla net.minecraft.data.worldgen.placement.OrePlacements
//Vanilla reads MainTarget.DEFAULT_HEIGHT (320) for the emerald upper bound; the literal stands in here
public static class OrePlacements
{
    private const int EmeraldUpperBound = 320;

    public static readonly ResourceKey<RegistryPlacedFeature> OreMagma = PlacementUtils.CreateKey("ore_magma");
    public static readonly ResourceKey<RegistryPlacedFeature> OreSoulSand = PlacementUtils.CreateKey("ore_soul_sand");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGoldDeltas =
        PlacementUtils.CreateKey("ore_gold_deltas");
    public static readonly ResourceKey<RegistryPlacedFeature> OreQuartzDeltas =
        PlacementUtils.CreateKey("ore_quartz_deltas");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGoldNether =
        PlacementUtils.CreateKey("ore_gold_nether");
    public static readonly ResourceKey<RegistryPlacedFeature> OreQuartzNether =
        PlacementUtils.CreateKey("ore_quartz_nether");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGravelNether =
        PlacementUtils.CreateKey("ore_gravel_nether");
    public static readonly ResourceKey<RegistryPlacedFeature> OreBlackstone =
        PlacementUtils.CreateKey("ore_blackstone");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDirt = PlacementUtils.CreateKey("ore_dirt");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGravel = PlacementUtils.CreateKey("ore_gravel");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGraniteUpper =
        PlacementUtils.CreateKey("ore_granite_upper");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGraniteLower =
        PlacementUtils.CreateKey("ore_granite_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDioriteUpper =
        PlacementUtils.CreateKey("ore_diorite_upper");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDioriteLower =
        PlacementUtils.CreateKey("ore_diorite_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> OreAndesiteUpper =
        PlacementUtils.CreateKey("ore_andesite_upper");
    public static readonly ResourceKey<RegistryPlacedFeature> OreAndesiteLower =
        PlacementUtils.CreateKey("ore_andesite_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> OreTuff = PlacementUtils.CreateKey("ore_tuff");
    public static readonly ResourceKey<RegistryPlacedFeature> OreCoalUpper =
        PlacementUtils.CreateKey("ore_coal_upper");
    public static readonly ResourceKey<RegistryPlacedFeature> OreCoalLower =
        PlacementUtils.CreateKey("ore_coal_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> OreIronUpper =
        PlacementUtils.CreateKey("ore_iron_upper");
    public static readonly ResourceKey<RegistryPlacedFeature> OreIronMiddle =
        PlacementUtils.CreateKey("ore_iron_middle");
    public static readonly ResourceKey<RegistryPlacedFeature> OreIronSmall =
        PlacementUtils.CreateKey("ore_iron_small");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGoldExtra =
        PlacementUtils.CreateKey("ore_gold_extra");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGold = PlacementUtils.CreateKey("ore_gold");
    public static readonly ResourceKey<RegistryPlacedFeature> OreGoldLower =
        PlacementUtils.CreateKey("ore_gold_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> OreRedstone =
        PlacementUtils.CreateKey("ore_redstone");
    public static readonly ResourceKey<RegistryPlacedFeature> OreRedstoneLower =
        PlacementUtils.CreateKey("ore_redstone_lower");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDiamond = PlacementUtils.CreateKey("ore_diamond");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDiamondMedium =
        PlacementUtils.CreateKey("ore_diamond_medium");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDiamondLarge =
        PlacementUtils.CreateKey("ore_diamond_large");
    public static readonly ResourceKey<RegistryPlacedFeature> OreDiamondBuried =
        PlacementUtils.CreateKey("ore_diamond_buried");
    public static readonly ResourceKey<RegistryPlacedFeature> OreLapis = PlacementUtils.CreateKey("ore_lapis");
    public static readonly ResourceKey<RegistryPlacedFeature> OreLapisBuried =
        PlacementUtils.CreateKey("ore_lapis_buried");
    public static readonly ResourceKey<RegistryPlacedFeature> OreInfested =
        PlacementUtils.CreateKey("ore_infested");
    public static readonly ResourceKey<RegistryPlacedFeature> OreEmerald = PlacementUtils.CreateKey("ore_emerald");
    public static readonly ResourceKey<RegistryPlacedFeature> OreAncientDebrisLarge =
        PlacementUtils.CreateKey("ore_ancient_debris_large");
    public static readonly ResourceKey<RegistryPlacedFeature> OreAncientDebrisSmall =
        PlacementUtils.CreateKey("ore_debris_small");
    public static readonly ResourceKey<RegistryPlacedFeature> OreCopper = PlacementUtils.CreateKey("ore_copper");
    public static readonly ResourceKey<RegistryPlacedFeature> OreCopperLarge =
        PlacementUtils.CreateKey("ore_copper_large");
    public static readonly ResourceKey<RegistryPlacedFeature> OreClay = PlacementUtils.CreateKey("ore_clay");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var oreMagma = features.GetOrThrow(OreFeatures.OreMagma);
        var oreSoulSand = features.GetOrThrow(OreFeatures.OreSoulSand);
        var oreNetherGold = features.GetOrThrow(OreFeatures.OreNetherGold);
        var oreQuartz = features.GetOrThrow(OreFeatures.OreQuartz);
        var oreGravelNether = features.GetOrThrow(OreFeatures.OreGravelNether);
        var oreBlackstone = features.GetOrThrow(OreFeatures.OreBlackstone);
        var oreDirt = features.GetOrThrow(OreFeatures.OreDirt);
        var oreGravel = features.GetOrThrow(OreFeatures.OreGravel);
        var oreGranite = features.GetOrThrow(OreFeatures.OreGranite);
        var oreDiorite = features.GetOrThrow(OreFeatures.OreDiorite);
        var oreAndesite = features.GetOrThrow(OreFeatures.OreAndesite);
        var oreTuff = features.GetOrThrow(OreFeatures.OreTuff);
        var oreCoal = features.GetOrThrow(OreFeatures.OreCoal);
        var oreCoalBuried = features.GetOrThrow(OreFeatures.OreCoalBuried);
        var oreIron = features.GetOrThrow(OreFeatures.OreIron);
        var oreIronSmall = features.GetOrThrow(OreFeatures.OreIronSmall);
        var oreGold = features.GetOrThrow(OreFeatures.OreGold);
        var oreGoldBuried = features.GetOrThrow(OreFeatures.OreGoldBuried);
        var oreRedstone = features.GetOrThrow(OreFeatures.OreRedstone);
        var oreDiamondSmall = features.GetOrThrow(OreFeatures.OreDiamondSmall);
        var oreDiamondMedium = features.GetOrThrow(OreFeatures.OreDiamondMedium);
        var oreDiamondLarge = features.GetOrThrow(OreFeatures.OreDiamondLarge);
        var oreDiamondBuried = features.GetOrThrow(OreFeatures.OreDiamondBuried);
        var oreLapis = features.GetOrThrow(OreFeatures.OreLapis);
        var oreLapisBuried = features.GetOrThrow(OreFeatures.OreLapisBuried);
        var oreInfested = features.GetOrThrow(OreFeatures.OreInfested);
        var oreEmerald = features.GetOrThrow(OreFeatures.OreEmerald);
        var oreAncientDebrisLarge = features.GetOrThrow(OreFeatures.OreAncientDebrisLarge);
        var oreAncientDebrisSmall = features.GetOrThrow(OreFeatures.OreAncientDebrisSmall);
        var oreCopperSmall = features.GetOrThrow(OreFeatures.OreCopperSmall);
        var oreCopperLarge = features.GetOrThrow(OreFeatures.OreCopperLarge);
        var oreClay = features.GetOrThrow(OreFeatures.OreClay);
        PlacementUtils.Register(context, OreMagma, oreMagma,
            CommonOrePlacement(4, Uniform(VerticalAnchor.Absolute(27), VerticalAnchor.Absolute(36))));
        PlacementUtils.Register(context, OreSoulSand, oreSoulSand,
            CommonOrePlacement(12, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(31))));
        PlacementUtils.Register(context, OreGoldDeltas, oreNetherGold, CommonOrePlacement(20, PlacementUtils.Range10To10));
        PlacementUtils.Register(context, OreQuartzDeltas, oreQuartz, CommonOrePlacement(32, PlacementUtils.Range10To10));
        PlacementUtils.Register(context, OreGoldNether, oreNetherGold, CommonOrePlacement(10, PlacementUtils.Range10To10));
        PlacementUtils.Register(context, OreQuartzNether, oreQuartz, CommonOrePlacement(16, PlacementUtils.Range10To10));
        PlacementUtils.Register(context, OreGravelNether, oreGravelNether,
            CommonOrePlacement(2, Uniform(VerticalAnchor.Absolute(5), VerticalAnchor.Absolute(41))));
        PlacementUtils.Register(context, OreBlackstone, oreBlackstone,
            CommonOrePlacement(2, Uniform(VerticalAnchor.Absolute(5), VerticalAnchor.Absolute(31))));
        PlacementUtils.Register(context, OreDirt, oreDirt,
            CommonOrePlacement(7, Uniform(VerticalAnchor.Absolute(0), VerticalAnchor.Absolute(160))));
        PlacementUtils.Register(context, OreGravel, oreGravel,
            CommonOrePlacement(14, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.BelowTop(0))));
        PlacementUtils.Register(context, OreGraniteUpper, oreGranite,
            RareOrePlacement(6, Uniform(VerticalAnchor.Absolute(64), VerticalAnchor.Absolute(128))));
        PlacementUtils.Register(context, OreGraniteLower, oreGranite,
            CommonOrePlacement(2, Uniform(VerticalAnchor.Absolute(0), VerticalAnchor.Absolute(60))));
        PlacementUtils.Register(context, OreDioriteUpper, oreDiorite,
            RareOrePlacement(6, Uniform(VerticalAnchor.Absolute(64), VerticalAnchor.Absolute(128))));
        PlacementUtils.Register(context, OreDioriteLower, oreDiorite,
            CommonOrePlacement(2, Uniform(VerticalAnchor.Absolute(0), VerticalAnchor.Absolute(60))));
        PlacementUtils.Register(context, OreAndesiteUpper, oreAndesite,
            RareOrePlacement(6, Uniform(VerticalAnchor.Absolute(64), VerticalAnchor.Absolute(128))));
        PlacementUtils.Register(context, OreAndesiteLower, oreAndesite,
            CommonOrePlacement(2, Uniform(VerticalAnchor.Absolute(0), VerticalAnchor.Absolute(60))));
        PlacementUtils.Register(context, OreTuff, oreTuff,
            CommonOrePlacement(2, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(0))));
        PlacementUtils.Register(context, OreCoalUpper, oreCoal,
            CommonOrePlacement(30, Uniform(VerticalAnchor.Absolute(136), VerticalAnchor.BelowTop(0))));
        PlacementUtils.Register(context, OreCoalLower, oreCoalBuried,
            CommonOrePlacement(20, Triangle(VerticalAnchor.Absolute(0), VerticalAnchor.Absolute(192))));
        PlacementUtils.Register(context, OreIronUpper, oreIron,
            CommonOrePlacement(90, Triangle(VerticalAnchor.Absolute(80), VerticalAnchor.Absolute(384))));
        PlacementUtils.Register(context, OreIronMiddle, oreIron,
            CommonOrePlacement(10, Triangle(VerticalAnchor.Absolute(-24), VerticalAnchor.Absolute(56))));
        PlacementUtils.Register(context, OreIronSmall, oreIronSmall,
            CommonOrePlacement(10, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(72))));
        PlacementUtils.Register(context, OreGoldExtra, oreGold,
            CommonOrePlacement(50, Uniform(VerticalAnchor.Absolute(32), VerticalAnchor.Absolute(256))));
        PlacementUtils.Register(context, OreGold, oreGoldBuried,
            CommonOrePlacement(4, Triangle(VerticalAnchor.Absolute(-64), VerticalAnchor.Absolute(32))));
        PlacementUtils.Register(context, OreGoldLower, oreGoldBuried,
            OrePlacement(CountPlacement.Of(UniformInt.Of(0, 1)),
                Uniform(VerticalAnchor.Absolute(-64), VerticalAnchor.Absolute(-48))));
        PlacementUtils.Register(context, OreRedstone, oreRedstone,
            CommonOrePlacement(4, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(15))));
        PlacementUtils.Register(context, OreRedstoneLower, oreRedstone,
            CommonOrePlacement(8, Triangle(VerticalAnchor.AboveBottom(-32), VerticalAnchor.AboveBottom(32))));
        PlacementUtils.Register(context, OreDiamond, oreDiamondSmall,
            CommonOrePlacement(7, Triangle(VerticalAnchor.AboveBottom(-80), VerticalAnchor.AboveBottom(80))));
        PlacementUtils.Register(context, OreDiamondMedium, oreDiamondMedium,
            CommonOrePlacement(2, Uniform(VerticalAnchor.Absolute(-64), VerticalAnchor.Absolute(-4))));
        PlacementUtils.Register(context, OreDiamondLarge, oreDiamondLarge,
            RareOrePlacement(9, Triangle(VerticalAnchor.AboveBottom(-80), VerticalAnchor.AboveBottom(80))));
        PlacementUtils.Register(context, OreDiamondBuried, oreDiamondBuried,
            CommonOrePlacement(4, Triangle(VerticalAnchor.AboveBottom(-80), VerticalAnchor.AboveBottom(80))));
        PlacementUtils.Register(context, OreLapis, oreLapis,
            CommonOrePlacement(2, Triangle(VerticalAnchor.Absolute(-32), VerticalAnchor.Absolute(32))));
        PlacementUtils.Register(context, OreLapisBuried, oreLapisBuried,
            CommonOrePlacement(4, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(64))));
        PlacementUtils.Register(context, OreInfested, oreInfested,
            CommonOrePlacement(14, Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(63))));
        PlacementUtils.Register(context, OreEmerald, oreEmerald,
            CommonOrePlacement(100, Triangle(VerticalAnchor.Absolute(-16), VerticalAnchor.Absolute(EmeraldUpperBound))));
        PlacementUtils.Register(context, OreAncientDebrisLarge, oreAncientDebrisLarge, InSquarePlacement.Spread(),
            Triangle(VerticalAnchor.Absolute(8), VerticalAnchor.Absolute(24)), BiomeFilter.Biome());
        PlacementUtils.Register(context, OreAncientDebrisSmall, oreAncientDebrisSmall, InSquarePlacement.Spread(),
            PlacementUtils.Range8To8, BiomeFilter.Biome());
        PlacementUtils.Register(context, OreCopper, oreCopperSmall,
            CommonOrePlacement(16, Triangle(VerticalAnchor.Absolute(-16), VerticalAnchor.Absolute(112))));
        PlacementUtils.Register(context, OreCopperLarge, oreCopperLarge,
            CommonOrePlacement(16, Triangle(VerticalAnchor.Absolute(-16), VerticalAnchor.Absolute(112))));
        PlacementUtils.Register(context, OreClay, oreClay,
            CommonOrePlacement(46, PlacementUtils.RangeBottomToMaxTerrainHeight));
    }

    private static PlacementModifier[] OrePlacement(PlacementModifier frequencyModifier, PlacementModifier heightRange)
        => [frequencyModifier, InSquarePlacement.Spread(), heightRange, BiomeFilter.Biome()];

    private static PlacementModifier[] CommonOrePlacement(int count, PlacementModifier heightRange)
        => OrePlacement(CountPlacement.Of(count), heightRange);

    private static PlacementModifier[] RareOrePlacement(int rarity, PlacementModifier heightRange)
        => OrePlacement(RarityFilter.OnAverageOnceEvery(rarity), heightRange);

    //Uniform a uniform height range, maps to vanilla HeightRangePlacement.uniform
    private static PlacementModifier Uniform(VerticalAnchor minInclusive, VerticalAnchor maxInclusive)
        => HeightRangePlacement.Uniform(minInclusive, maxInclusive);

    //Triangle a triangular height distribution, maps to vanilla HeightRangePlacement.triangle
    private static PlacementModifier Triangle(VerticalAnchor minInclusive, VerticalAnchor maxInclusive)
        => HeightRangePlacement.Of(TrapezoidHeight.Of(minInclusive, maxInclusive));
}
