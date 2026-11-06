using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using Direction = NacreCraft.Primitives.Direction;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//NetherPlacements built-in placed nether features, maps to vanilla net.minecraft.data.worldgen.placement.NetherPlacements
public static class NetherPlacements
{
    public static readonly ResourceKey<RegistryPlacedFeature> Delta = PlacementUtils.CreateKey("delta");
    public static readonly ResourceKey<RegistryPlacedFeature> SmallBasaltColumns =
        PlacementUtils.CreateKey("small_basalt_columns");
    public static readonly ResourceKey<RegistryPlacedFeature> LargeBasaltColumns =
        PlacementUtils.CreateKey("large_basalt_columns");
    public static readonly ResourceKey<RegistryPlacedFeature> BasaltBlobs = PlacementUtils.CreateKey("basalt_blobs");
    public static readonly ResourceKey<RegistryPlacedFeature> BlackstoneBlobs =
        PlacementUtils.CreateKey("blackstone_blobs");
    public static readonly ResourceKey<RegistryPlacedFeature> GlowstoneExtra =
        PlacementUtils.CreateKey("glowstone_extra");
    public static readonly ResourceKey<RegistryPlacedFeature> Glowstone = PlacementUtils.CreateKey("glowstone");
    public static readonly ResourceKey<RegistryPlacedFeature> CrimsonForestVegetation =
        PlacementUtils.CreateKey("crimson_forest_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> WarpedForestVegetation =
        PlacementUtils.CreateKey("warped_forest_vegetation");
    public static readonly ResourceKey<RegistryPlacedFeature> NetherSprouts =
        PlacementUtils.CreateKey("nether_sprouts");
    public static readonly ResourceKey<RegistryPlacedFeature> TwistingVines =
        PlacementUtils.CreateKey("twisting_vines");
    public static readonly ResourceKey<RegistryPlacedFeature> WeepingVines =
        PlacementUtils.CreateKey("weeping_vines");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchCrimsonRoots =
        PlacementUtils.CreateKey("patch_crimson_roots");
    public static readonly ResourceKey<RegistryPlacedFeature> BasaltPillar =
        PlacementUtils.CreateKey("basalt_pillar");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringDelta = PlacementUtils.CreateKey("spring_delta");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringClosed =
        PlacementUtils.CreateKey("spring_closed");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringClosedDouble =
        PlacementUtils.CreateKey("spring_closed_double");
    public static readonly ResourceKey<RegistryPlacedFeature> SpringOpen = PlacementUtils.CreateKey("spring_open");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchSoulFire =
        PlacementUtils.CreateKey("patch_soul_fire");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchFire = PlacementUtils.CreateKey("patch_fire");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var delta = features.GetOrThrow(NetherFeatures.Delta);
        var smallBasaltColumns = features.GetOrThrow(NetherFeatures.SmallBasaltColumns);
        var largeBasaltColumns = features.GetOrThrow(NetherFeatures.LargeBasaltColumns);
        var basaltBlobs = features.GetOrThrow(NetherFeatures.BasaltBlobs);
        var blackstoneBlobs = features.GetOrThrow(NetherFeatures.BlackstoneBlobs);
        var glowstoneExtra = features.GetOrThrow(NetherFeatures.GlowstoneExtra);
        var crimsonForestVegetation = features.GetOrThrow(NetherFeatures.CrimsonForestVegetation);
        var warpedForestVegetation = features.GetOrThrow(NetherFeatures.WarpedForestVegetation);
        var netherSprouts = features.GetOrThrow(NetherFeatures.NetherSprouts);
        var twistingVines = features.GetOrThrow(NetherFeatures.TwistingVines);
        var weepingVines = features.GetOrThrow(NetherFeatures.WeepingVines);
        var crimsonRoots = features.GetOrThrow(NetherFeatures.CrimsonRoots);
        var basaltPillar = features.GetOrThrow(NetherFeatures.BasaltPillar);
        var springLavaNether = features.GetOrThrow(NetherFeatures.SpringLavaNether);
        var springNetherClosed = features.GetOrThrow(NetherFeatures.SpringNetherClosed);
        var springNetherOpen = features.GetOrThrow(NetherFeatures.SpringNetherOpen);
        var soulFire = features.GetOrThrow(NetherFeatures.SoulFire);
        var fire = features.GetOrThrow(NetherFeatures.Fire);
        PlacementUtils.Register(context, Delta, delta, CountOnEveryLayerPlacement.Of(40), BiomeFilter.Biome());
        PlacementUtils.Register(context, SmallBasaltColumns, smallBasaltColumns,
            CountOnEveryLayerPlacement.Of(4), BiomeFilter.Biome());
        PlacementUtils.Register(context, LargeBasaltColumns, largeBasaltColumns,
            CountOnEveryLayerPlacement.Of(2), BiomeFilter.Biome());
        PlacementUtils.Register(context, BasaltBlobs, basaltBlobs, CountPlacement.Of(75), InSquarePlacement.Spread(),
            PlacementUtils.FullRange, BiomeFilter.Biome());
        PlacementUtils.Register(context, BlackstoneBlobs, blackstoneBlobs, CountPlacement.Of(25),
            InSquarePlacement.Spread(), PlacementUtils.FullRange, BiomeFilter.Biome());
        PlacementUtils.Register(context, GlowstoneExtra, glowstoneExtra,
            CountPlacement.Of(BiasedToBottomInt.Of(0, 9)), InSquarePlacement.Spread(), PlacementUtils.Range4To4,
            BiomeFilter.Biome());
        PlacementUtils.Register(context, Glowstone, glowstoneExtra, CountPlacement.Of(10), InSquarePlacement.Spread(),
            PlacementUtils.FullRange, BiomeFilter.Biome());
        PlacementUtils.Register(context, CrimsonForestVegetation, crimsonForestVegetation,
            CountOnEveryLayerPlacement.Of(6), BiomeFilter.Biome());
        PlacementUtils.Register(context, WarpedForestVegetation, warpedForestVegetation,
            CountOnEveryLayerPlacement.Of(5), BiomeFilter.Biome());
        PlacementUtils.Register(context, NetherSprouts, netherSprouts, CountOnEveryLayerPlacement.Of(4),
            BiomeFilter.Biome());
        PlacementUtils.Register(context, TwistingVines, twistingVines, CountPlacement.Of(10),
            InSquarePlacement.Spread(), PlacementUtils.FullRange, BiomeFilter.Biome());
        PlacementUtils.Register(context, WeepingVines, weepingVines, CountPlacement.Of(10),
            InSquarePlacement.Spread(), PlacementUtils.FullRange, BiomeFilter.Biome());
        PlacementUtils.Register(context, PatchCrimsonRoots, crimsonRoots, PlacementUtils.FullRange,
            BiomeFilter.Biome(), CountPlacement.Of(96), RandomOffsetPlacement.OfTriangle(7, 3),
            BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir));
        PlacementUtils.Register(context, BasaltPillar, basaltPillar, CountPlacement.Of(10),
            InSquarePlacement.Spread(), PlacementUtils.FullRange, BiomeFilter.Biome());
        PlacementUtils.Register(context, SpringDelta, springLavaNether, CountPlacement.Of(16),
            InSquarePlacement.Spread(), PlacementUtils.Range4To4, BiomeFilter.Biome());
        PlacementUtils.Register(context, SpringClosed, springNetherClosed, CountPlacement.Of(16),
            InSquarePlacement.Spread(), PlacementUtils.Range10To10, BiomeFilter.Biome());
        PlacementUtils.Register(context, SpringClosedDouble, springNetherClosed, CountPlacement.Of(32),
            InSquarePlacement.Spread(), PlacementUtils.Range10To10, BiomeFilter.Biome());
        PlacementUtils.Register(context, SpringOpen, springNetherOpen, CountPlacement.Of(8),
            InSquarePlacement.Spread(), PlacementUtils.Range4To4, BiomeFilter.Biome());
        PlacementUtils.Register(context, PatchSoulFire, soulFire, FirePlacement("soul_soil"));
        PlacementUtils.Register(context, PatchFire, fire, FirePlacement("netherrack"));
    }

    //FirePlacement the fire patch chain that pins the fuel block below, maps to vanilla firePlacement
    private static PlacementModifier[] FirePlacement(string onlyOnBlock) =>
    [
        CountPlacement.Of(UniformInt.Of(0, 5)), InSquarePlacement.Spread(), PlacementUtils.Range4To4,
        BiomeFilter.Biome(), CountPlacement.Of(96), RandomOffsetPlacement.OfTriangle(7, 3),
        BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
            BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0), WorldGenDataSupport.Block(onlyOnBlock))))
    ];
}
