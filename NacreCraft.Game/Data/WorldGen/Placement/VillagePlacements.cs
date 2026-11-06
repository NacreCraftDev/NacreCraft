using NacreCraft.Game.Data.WorldGen.Features;
using NacreCraft.Game.World.Level.LevelGen.BlockPredicates;
using NacreCraft.Game.World.Level.LevelGen.Placement;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using Direction = NacreCraft.Primitives.Direction;
using RegistryPlacedFeature = NacreCraft.Registry.PlacedFeature;

namespace NacreCraft.Game.Data.WorldGen.Placement;

//VillagePlacements built-in placed village decorations, maps to vanilla net.minecraft.data.worldgen.placement.VillagePlacements
public static class VillagePlacements
{
    public static readonly ResourceKey<RegistryPlacedFeature> PileHayVillage = PlacementUtils.CreateKey("pile_hay");
    public static readonly ResourceKey<RegistryPlacedFeature> PileMelonVillage =
        PlacementUtils.CreateKey("pile_melon");
    public static readonly ResourceKey<RegistryPlacedFeature> PileSnowVillage = PlacementUtils.CreateKey("pile_snow");
    public static readonly ResourceKey<RegistryPlacedFeature> PileIceVillage = PlacementUtils.CreateKey("pile_ice");
    public static readonly ResourceKey<RegistryPlacedFeature> PilePumpkinVillage =
        PlacementUtils.CreateKey("pile_pumpkin");
    public static readonly ResourceKey<RegistryPlacedFeature> OakVillage = PlacementUtils.CreateKey("oak");
    public static readonly ResourceKey<RegistryPlacedFeature> AcaciaVillage = PlacementUtils.CreateKey("acacia");
    public static readonly ResourceKey<RegistryPlacedFeature> SpruceVillage = PlacementUtils.CreateKey("spruce");
    public static readonly ResourceKey<RegistryPlacedFeature> PineVillage = PlacementUtils.CreateKey("pine");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchCactusVillage =
        PlacementUtils.CreateKey("patch_cactus");
    public static readonly ResourceKey<RegistryPlacedFeature> FlowerPlainVillage =
        PlacementUtils.CreateKey("flower_plain");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchTaigaGrassVillage =
        PlacementUtils.CreateKey("patch_taiga_grass");
    public static readonly ResourceKey<RegistryPlacedFeature> PatchBerryBushVillage =
        PlacementUtils.CreateKey("patch_berry_bush");

    public static void Bootstrap(BootstrapContext<RegistryPlacedFeature> context)
    {
        var features = context.LookupOrThrow(Registries.CONFIGURED_FEATURE);
        var pileHay = features.GetOrThrow(PileFeatures.PileHay);
        var pileMelon = features.GetOrThrow(PileFeatures.PileMelon);
        var pileSnow = features.GetOrThrow(PileFeatures.PileSnow);
        var pileIce = features.GetOrThrow(PileFeatures.PileIce);
        var pilePumpkin = features.GetOrThrow(PileFeatures.PilePumpkin);
        var oak = features.GetOrThrow(TreeFeatures.Oak);
        var acacia = features.GetOrThrow(TreeFeatures.Acacia);
        var spruce = features.GetOrThrow(TreeFeatures.Spruce);
        var pine = features.GetOrThrow(TreeFeatures.Pine);
        var cactus = features.GetOrThrow(VegetationFeatures.Cactus);
        var flowerPlain = features.GetOrThrow(VegetationFeatures.FlowerPlain);
        var taigaGrass = features.GetOrThrow(VegetationFeatures.TaigaGrass);
        var berryBush = features.GetOrThrow(VegetationFeatures.BerryBush);
        PlacementUtils.Register(context, PileHayVillage, pileHay, Array.Empty<PlacementModifier>());
        PlacementUtils.Register(context, PileMelonVillage, pileMelon, Array.Empty<PlacementModifier>());
        PlacementUtils.Register(context, PileSnowVillage, pileSnow, Array.Empty<PlacementModifier>());
        PlacementUtils.Register(context, PileIceVillage, pileIce, Array.Empty<PlacementModifier>());
        PlacementUtils.Register(context, PilePumpkinVillage, pilePumpkin, Array.Empty<PlacementModifier>());
        PlacementUtils.Register(context, OakVillage, oak,
            PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("oak_sapling")));
        PlacementUtils.Register(context, AcaciaVillage, acacia,
            PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("acacia_sapling")));
        PlacementUtils.Register(context, SpruceVillage, spruce,
            PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, PineVillage, pine,
            PlacementUtils.FilteredByBlockSurvival(WorldGenDataSupport.Block("spruce_sapling")));
        PlacementUtils.Register(context, PatchCactusVillage, cactus, CountPlacement.Of(10),
            RandomOffsetPlacement.OfTriangle(7, 3),
            BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
                BlockPredicate.WouldSurvive(WorldGenDataSupport.State("cactus"), Vec3i.Zero))));
        PlacementUtils.Register(context, FlowerPlainVillage, flowerPlain, CountPlacement.Of(64),
            RandomOffsetPlacement.OfTriangle(6, 2),
            BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir));
        PlacementUtils.Register(context, PatchTaigaGrassVillage, taigaGrass, CountPlacement.Of(32),
            RandomOffsetPlacement.OfTriangle(7, 3),
            BlockPredicateFilter.ForPredicate(BlockPredicate.OnlyInAir));
        PlacementUtils.Register(context, PatchBerryBushVillage, berryBush, CountPlacement.Of(96),
            RandomOffsetPlacement.OfTriangle(7, 3),
            BlockPredicateFilter.ForPredicate(BlockPredicate.AllOf(BlockPredicate.OnlyInAir,
                BlockPredicate.MatchesBlocks(new Vec3i(0, -1, 0), WorldGenDataSupport.Block("grass_block")))));
    }
}
