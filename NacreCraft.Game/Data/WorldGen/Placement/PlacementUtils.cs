using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Features;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util.Random;
using GameConfiguredFeature = NacreCraft.Game.World.Level.LevelGen.Features.ConfiguredFeature;
using GameFeature = NacreCraft.Game.World.Level.LevelGen.Features.Feature;
using GamePlacedFeature = NacreCraft.Game.World.Level.LevelGen.Placement.PlacedFeature;
using HeightmapTypes = NacreCraft.Registry.Heightmap.Types;
using RegistryConfiguredFeature = NacreCraft.Registry.ConfiguredFeature;
using GameIntProvider = NacreCraft.Game.World.Level.LevelGen.IntProvider;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//PlacementUtils shared placed feature construction, maps to vanilla net.minecraft.data.worldgen.placement.PlacementUtils
//Holds the reusable heightmap/height range modifiers and the helpers the placement data classes register through
//The aggregate Bootstrap lands together with the nine placement data classes, which are the next slice
public static class PlacementUtils
{
    public static readonly PlacementModifier Heightmap = HeightmapPlacement.OnHeightmap(HeightmapTypes.MotionBlocking);
    public static readonly PlacementModifier HeightmapNoLeaves =
        HeightmapPlacement.OnHeightmap(HeightmapTypes.MotionBlockingNoLeaves);
    public static readonly PlacementModifier HeightmapTopSolid =
        HeightmapPlacement.OnHeightmap(HeightmapTypes.OceanFloorWg);
    public static readonly PlacementModifier HeightmapWorldSurface =
        HeightmapPlacement.OnHeightmap(HeightmapTypes.WorldSurfaceWg);
    public static readonly PlacementModifier HeightmapOceanFloor =
        HeightmapPlacement.OnHeightmap(HeightmapTypes.OceanFloor);
    public static readonly PlacementModifier FullRange =
        HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.BelowTop(0));
    public static readonly PlacementModifier Range10To10 =
        HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(10), VerticalAnchor.BelowTop(10));
    public static readonly PlacementModifier Range8To8 =
        HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(8), VerticalAnchor.BelowTop(8));
    public static readonly PlacementModifier Range4To4 =
        HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(4), VerticalAnchor.BelowTop(4));
    public static readonly PlacementModifier RangeBottomToMaxTerrainHeight =
        HeightRangePlacement.Uniform(VerticalAnchor.AboveBottom(0), VerticalAnchor.Absolute(256));

    public static ResourceKey<RegistryPlacedFeature> CreateKey(string name)
        => ResourceKey<RegistryPlacedFeature>.Create(Registries.PLACED_FEATURE, Identifier.WithDefaultNamespace(name));

    //Bootstrap registers every built-in placed feature data class, maps to vanilla bootstrap
    //CavePlacements joins this list once CaveFeatures (and its ProcessorLists dependency) is ported
    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        AquaticPlacements.Bootstrap(context);
        EndPlacements.Bootstrap(context);
        MiscOverworldPlacements.Bootstrap(context);
        NetherPlacements.Bootstrap(context);
        OrePlacements.Bootstrap(context);
        TreePlacements.Bootstrap(context);
        VegetationPlacements.Bootstrap(context);
        VillagePlacements.Bootstrap(context);
    }

    public static void Register(BootstrapContext<RegistryPlacedFeature> context, ResourceKey<RegistryPlacedFeature> id,
        Holder<RegistryConfiguredFeature> feature, IReadOnlyList<PlacementModifier> placementModifiers)
        => context.Register(id, new GamePlacedFeature(feature, placementModifiers));

    public static void Register(BootstrapContext<RegistryPlacedFeature> context, ResourceKey<RegistryPlacedFeature> id,
        Holder<RegistryConfiguredFeature> feature, params PlacementModifier[] placementModifiers)
        => Register(context, id, feature, (IReadOnlyList<PlacementModifier>)placementModifiers);

    //CountExtra count plus a chance of extra more, maps to vanilla countExtra
    //The chance must be representable as an integer weight, otherwise vanilla throws
    public static PlacementModifier CountExtra(int count, float chance, int extra)
    {
        var weight = 1.0f / chance;
        if (Math.Abs(weight - (int)weight) > 1.0E-5f)
            throw new InvalidOperationException("Chance data cannot be represented as list weight");
        var distribution = WeightedList<GameIntProvider>.Builder()
            .Add(ConstantInt.Of(count), (int)weight - 1)
            .Add(ConstantInt.Of(count + extra), 1)
            .Build();
        return CountPlacement.Of(new WeightedListInt(distribution));
    }

    public static PlacementFilter IsEmpty() => BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir);

    public static BlockPredicateFilter FilteredByBlockSurvival(NacreCraft.Registry.Block block)
        => BlockPredicateFilter.ForPredicate(BlockPredicate.WouldSurvive(block.DefaultBlockState, Vec3i.Zero));

    public static Holder<RegistryPlacedFeature> InlinePlaced(Holder<RegistryConfiguredFeature> configuredFeature,
        params PlacementModifier[] placedFeatures)
        => Holder<RegistryPlacedFeature>.Direct(new GamePlacedFeature(configuredFeature, placedFeatures));

    public static Holder<RegistryPlacedFeature> InlinePlaced(GameFeature feature, FeatureConfiguration config,
        params PlacementModifier[] placedFeatures)
        => InlinePlaced(Holder<RegistryConfiguredFeature>.Direct(new GameConfiguredFeature(feature, config)),
            placedFeatures);

    public static Holder<RegistryPlacedFeature> OnlyWhenEmpty(GameFeature feature, FeatureConfiguration config)
        => Filtered(feature, config, BlockPredicate.OnlyInAir);

    public static Holder<RegistryPlacedFeature> Filtered(GameFeature feature, FeatureConfiguration config,
        BlockPredicate predicate)
        => InlinePlaced(feature, config, BlockPredicateFilter.ForPredicate(predicate));
}
