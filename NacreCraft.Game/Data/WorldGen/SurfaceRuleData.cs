using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Synth;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using RegBlock = NacreCraft.Registry.Block;

namespace NacreCraft.Game.Data.WorldGen;

//SurfaceRuleData built-in surface rules, maps to vanilla net.minecraft.data.worldgen.SurfaceRuleData
//The surface rule system stores rules as plain constructors here, so the vanilla static factories are rebuilt as local helpers
public static class SurfaceRuleData
{
    private static readonly SurfaceRules.RuleSource Air = State("air");
    private static readonly SurfaceRules.RuleSource Bedrock = State("bedrock");
    private static readonly SurfaceRules.RuleSource WhiteTerracotta = State("white_terracotta");
    private static readonly SurfaceRules.RuleSource OrangeTerracotta = State("orange_terracotta");
    private static readonly SurfaceRules.RuleSource Terracotta = State("terracotta");
    private static readonly SurfaceRules.RuleSource RedSand = State("red_sand");
    private static readonly SurfaceRules.RuleSource RedSandstone = State("red_sandstone");
    private static readonly SurfaceRules.RuleSource Stone = State("stone");
    private static readonly SurfaceRules.RuleSource Deepslate = State("deepslate");
    private static readonly SurfaceRules.RuleSource Dirt = State("dirt");
    private static readonly SurfaceRules.RuleSource Podzol = State("podzol");
    private static readonly SurfaceRules.RuleSource CoarseDirt = State("coarse_dirt");
    private static readonly SurfaceRules.RuleSource Mycelium = State("mycelium");
    private static readonly SurfaceRules.RuleSource GrassBlock = State("grass_block");
    private static readonly SurfaceRules.RuleSource Calcite = State("calcite");
    private static readonly SurfaceRules.RuleSource Gravel = State("gravel");
    private static readonly SurfaceRules.RuleSource Sand = State("sand");
    private static readonly SurfaceRules.RuleSource Sandstone = State("sandstone");
    private static readonly SurfaceRules.RuleSource PackedIce = State("packed_ice");
    private static readonly SurfaceRules.RuleSource SnowBlock = State("snow_block");
    private static readonly SurfaceRules.RuleSource Mud = State("mud");
    private static readonly SurfaceRules.RuleSource PowderSnow = State("powder_snow");
    private static readonly SurfaceRules.RuleSource Ice = State("ice");
    private static readonly SurfaceRules.RuleSource Water = State("water");
    private static readonly SurfaceRules.RuleSource Lava = State("lava");
    private static readonly SurfaceRules.RuleSource Netherrack = State("netherrack");
    private static readonly SurfaceRules.RuleSource SoulSand = State("soul_sand");
    private static readonly SurfaceRules.RuleSource SoulSoil = State("soul_soil");
    private static readonly SurfaceRules.RuleSource Basalt = State("basalt");
    private static readonly SurfaceRules.RuleSource Blackstone = State("blackstone");
    private static readonly SurfaceRules.RuleSource WarpedWartBlock = State("warped_wart_block");
    private static readonly SurfaceRules.RuleSource WarpedNylium = State("warped_nylium");
    private static readonly SurfaceRules.RuleSource NetherWartBlock = State("nether_wart_block");
    private static readonly SurfaceRules.RuleSource CrimsonNylium = State("crimson_nylium");
    private static readonly SurfaceRules.RuleSource Endstone = State("end_stone");
    private static readonly SurfaceRules.RuleSource Cinnabar = State("cinnabar");
    private static readonly SurfaceRules.RuleSource Sulfur = State("sulfur");

    private static readonly SurfaceRules.ConditionSource OnFloor =
        new SurfaceRules.StoneDepth(0, false, 0, SurfaceRules.SurfaceType.Floor);
    private static readonly SurfaceRules.ConditionSource UnderFloor =
        new SurfaceRules.StoneDepth(0, true, 0, SurfaceRules.SurfaceType.Floor);
    private static readonly SurfaceRules.ConditionSource DeepUnderFloor =
        new SurfaceRules.StoneDepth(0, true, 6, SurfaceRules.SurfaceType.Floor);
    private static readonly SurfaceRules.ConditionSource VeryDeepUnderFloor =
        new SurfaceRules.StoneDepth(0, true, 30, SurfaceRules.SurfaceType.Floor);
    private static readonly SurfaceRules.ConditionSource OnCeiling =
        new SurfaceRules.StoneDepth(0, false, 0, SurfaceRules.SurfaceType.Ceiling);
    private static readonly SurfaceRules.ConditionSource UnderCeiling =
        new SurfaceRules.StoneDepth(0, true, 0, SurfaceRules.SurfaceType.Ceiling);

    //State one fixed block state rule by registry path, maps to vanilla makeStateRule
    private static SurfaceRules.RuleSource State(string path)
        => new SurfaceRules.BlockStateRule(WorldGenDataSupport.State(path));

    //If applies the rule when the condition holds, maps to vanilla SurfaceRules.ifTrue
    private static SurfaceRules.RuleSource If(SurfaceRules.ConditionSource condition, SurfaceRules.RuleSource rule)
        => new SurfaceRules.IfTrue(condition, rule);

    //Seq first hit wins, maps to vanilla SurfaceRules.sequence
    private static SurfaceRules.RuleSource Seq(params SurfaceRules.RuleSource[] rules)
        => new SurfaceRules.Sequence(rules);

    private static SurfaceRules.ConditionSource Not(SurfaceRules.ConditionSource condition)
        => new SurfaceRules.Not(condition);

    //IsBiome matches any of the given biome keys, maps to vanilla SurfaceRules.isBiome
    private static SurfaceRules.ConditionSource IsBiome(HolderLookup<Biome> biomes, params ResourceKey<Biome>[] keys)
    {
        var registry = biomes as Registry<Biome>
                       ?? throw new InvalidOperationException("biome lookup must be a registry");
        return new SurfaceRules.BiomeCondition(
            keys.Select(key => (Holder<Biome>)registry.GetOrCreateHolder(key.Identifier)).ToList());
    }

    //YBlock a y check that does not count stone depth, maps to vanilla yBlockCheck
    private static SurfaceRules.ConditionSource YBlock(VerticalAnchor anchor, int surfaceDepthMultiplier)
        => new SurfaceRules.YAbove(anchor, surfaceDepthMultiplier, false);

    //YStart a y check that counts stone depth, maps to vanilla yStartCheck
    private static SurfaceRules.ConditionSource YStart(VerticalAnchor anchor, int surfaceDepthMultiplier)
        => new SurfaceRules.YAbove(anchor, surfaceDepthMultiplier, true);

    //WaterBlock a water level check without stone depth, maps to vanilla waterBlockCheck
    private static SurfaceRules.ConditionSource WaterBlock(int offset, int surfaceDepthMultiplier)
        => new SurfaceRules.Water(offset, false, surfaceDepthMultiplier);

    //WaterStart a water level check with stone depth, maps to vanilla waterStartCheck
    private static SurfaceRules.ConditionSource WaterStart(int offset, int surfaceDepthMultiplier)
        => new SurfaceRules.Water(offset, true, surfaceDepthMultiplier);

    //Noise2D 2D noise threshold, maps to vanilla noiseCondition2d
    private static SurfaceRules.ConditionSource Noise2D(ResourceKey<NoiseParameters> key, double min, double max)
        => new SurfaceRules.NoiseThreshold(NoiseHolder(key), min, max, false);

    private static SurfaceRules.ConditionSource Noise2D(ResourceKey<NoiseParameters> key, double min)
        => Noise2D(key, min, double.MaxValue);

    //Noise3D 3D noise threshold, maps to vanilla noiseCondition3d
    private static SurfaceRules.ConditionSource Noise3D(ResourceKey<NoiseParameters> key, double min, double max)
        => new SurfaceRules.NoiseThreshold(NoiseHolder(key), min, max, true);

    private static SurfaceRules.ConditionSource Noise3D(ResourceKey<NoiseParameters> key, double min)
        => Noise3D(key, min, double.MaxValue);

    //NoiseHolder resolves a noise parameter key to a holder; the placeholder is bound by Noises.Bootstrap
    private static NoiseHolder NoiseHolder(ResourceKey<NoiseParameters> key)
        => new(BuiltInRegistries.NOISE.GetOrCreateHolder(key.Identifier));

    //SurfaceNoiseAbove the scaled surface noise threshold, maps to vanilla surfaceNoiseAbove
    private static SurfaceRules.ConditionSource SurfaceNoiseAbove(double threshold)
        => Noise2D(Noises.Surface, threshold / 8.25, double.MaxValue);

    //VerticalGradient a probability band between two anchors, maps to vanilla verticalGradient
    private static SurfaceRules.ConditionSource VerticalGradient(string name, VerticalAnchor trueAtAndBelow,
        VerticalAnchor falseAtAndAbove)
        => new SurfaceRules.VerticalGradient(trueAtAndBelow, falseAtAndAbove, Identifier.WithDefaultNamespace(name));

    public static SurfaceRules.RuleSource Overworld(HolderLookup<Biome> biomes)
        => OverworldLike(biomes, true, false, true);

    public static SurfaceRules.RuleSource OverworldLike(HolderLookup<Biome> biomes, bool doPreliminarySurfaceCheck,
        bool bedrockRoof, bool bedrockFloor)
    {
        var woodedBadlandsTop = YBlock(VerticalAnchor.Absolute(97), 2);
        var badlandsTop = YBlock(VerticalAnchor.Absolute(256), 0);
        var badlandsHeightCondition = YStart(VerticalAnchor.Absolute(63), -1);
        var badlandsMid = YStart(VerticalAnchor.Absolute(74), 1);
        var mangroveSwampPuddleLevel = YBlock(VerticalAnchor.Absolute(60), 0);
        var swampPuddleLevel = YBlock(VerticalAnchor.Absolute(62), 0);
        var aboveOverworldSeaLevel = YBlock(VerticalAnchor.Absolute(63), 0);
        var notUnderwater = WaterBlock(-1, 0);
        var aboveWater = WaterBlock(0, 0);
        var notUnderDeepWater = WaterStart(-6, -1);
        var hole = new SurfaceRules.Hole();
        var frozenOcean = IsBiome(biomes, NacreCraft.Registry.Biomes.FROZEN_OCEAN, NacreCraft.Registry.Biomes.DEEP_FROZEN_OCEAN);
        var steep = new SurfaceRules.Steep();
        var grassOrDirtIfUnderwater = Seq(If(aboveWater, GrassBlock), Dirt);
        var sandOrSandstoneIfCeiling = Seq(If(OnCeiling, Sandstone), Sand);
        var gravelOrStoneIfCeiling = Seq(If(OnCeiling, Stone), Gravel);
        var biomesWithSandAndSandstone = IsBiome(biomes, NacreCraft.Registry.Biomes.WARM_OCEAN, NacreCraft.Registry.Biomes.BEACH, NacreCraft.Registry.Biomes.SNOWY_BEACH);
        var biomesWithSandAndVeryDeepSandstone = IsBiome(biomes, NacreCraft.Registry.Biomes.DESERT);
        var sulfurCaveBands = Seq(
            If(Noise3D(Noises.SulfurCaveGradient, -0.4000000059604645, -0.10000000149011612), Cinnabar),
            If(Noise3D(Noises.SulfurCaveGradient, 0.0, 0.4000000059604645), Sulfur),
            If(Noise3D(Noises.SulfurCaveGradient, 0.4000000059604645), Cinnabar));
        var commonSurfaceAndUnderRules = Seq(
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.STONY_PEAKS),
                Seq(If(Noise2D(Noises.Calcite, -0.0125, 0.0125), Calcite), Stone)),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.STONY_SHORE),
                Seq(If(Noise2D(Noises.Gravel, -0.05, 0.05), gravelOrStoneIfCeiling), Stone)),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.WINDSWEPT_HILLS), If(SurfaceNoiseAbove(1.0), Stone)),
            If(biomesWithSandAndSandstone, sandOrSandstoneIfCeiling),
            If(biomesWithSandAndVeryDeepSandstone, sandOrSandstoneIfCeiling),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.DRIPSTONE_CAVES), Stone),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.SULFUR_CAVES), Seq(sulfurCaveBands, Stone)));
        var powderSnowUnderRule = If(Noise2D(Noises.PowderSnow, 0.45, 0.58), If(aboveWater, PowderSnow));
        var powderSnowSurfaceRule = If(Noise2D(Noises.PowderSnow, 0.35, 0.6), If(aboveWater, PowderSnow));
        var biomeUnderSurfaceRule = Seq(
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.FROZEN_PEAKS), Seq(If(steep, PackedIce),
                If(Noise2D(Noises.PackedIce, -0.5, 0.2), PackedIce), If(Noise2D(Noises.Ice, -0.0625, 0.025), Ice),
                If(aboveWater, SnowBlock))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.SNOWY_SLOPES),
                Seq(If(steep, Stone), powderSnowUnderRule, If(aboveWater, SnowBlock))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.JAGGED_PEAKS), Stone),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.GROVE), Seq(powderSnowUnderRule, Dirt)),
            commonSurfaceAndUnderRules,
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.WINDSWEPT_SAVANNA), If(SurfaceNoiseAbove(1.75), Stone)),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.WINDSWEPT_GRAVELLY_HILLS), Seq(
                If(SurfaceNoiseAbove(2.0), gravelOrStoneIfCeiling), If(SurfaceNoiseAbove(1.0), Stone),
                If(SurfaceNoiseAbove(-1.0), Dirt), gravelOrStoneIfCeiling)),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.MANGROVE_SWAMP), Mud),
            Dirt);
        var biomeSurfaceRule = Seq(
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.FROZEN_PEAKS), Seq(If(steep, PackedIce),
                If(Noise2D(Noises.PackedIce, 0.0, 0.2), PackedIce), If(Noise2D(Noises.Ice, 0.0, 0.025), Ice),
                If(aboveWater, SnowBlock))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.SNOWY_SLOPES),
                Seq(If(steep, Stone), powderSnowSurfaceRule, If(aboveWater, SnowBlock))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.JAGGED_PEAKS),
                Seq(If(steep, Stone), If(aboveWater, SnowBlock))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.GROVE),
                Seq(powderSnowSurfaceRule, If(aboveWater, SnowBlock))),
            commonSurfaceAndUnderRules,
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.WINDSWEPT_SAVANNA), Seq(If(SurfaceNoiseAbove(1.75), Stone),
                If(SurfaceNoiseAbove(-0.5), CoarseDirt))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.WINDSWEPT_GRAVELLY_HILLS), Seq(
                If(SurfaceNoiseAbove(2.0), gravelOrStoneIfCeiling), If(SurfaceNoiseAbove(1.0), Stone),
                If(SurfaceNoiseAbove(-1.0), grassOrDirtIfUnderwater), gravelOrStoneIfCeiling)),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.OLD_GROWTH_PINE_TAIGA, NacreCraft.Registry.Biomes.OLD_GROWTH_SPRUCE_TAIGA),
                Seq(If(SurfaceNoiseAbove(1.75), CoarseDirt), If(SurfaceNoiseAbove(-0.95), Podzol))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.ICE_SPIKES), If(aboveWater, SnowBlock)),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.MANGROVE_SWAMP), Mud),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.MUSHROOM_FIELDS), Mycelium),
            grassOrDirtIfUnderwater);
        var clayBand1 = Noise2D(Noises.Surface, -0.909, -0.5454);
        var clayBand2 = Noise2D(Noises.Surface, -0.1818, 0.1818);
        var clayBand3 = Noise2D(Noises.Surface, 0.5454, 0.909);
        var mainRuleCloseToSurface = Seq(
            If(OnFloor, Seq(
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.WOODED_BADLANDS), If(woodedBadlandsTop, Seq(
                    If(clayBand1, CoarseDirt), If(clayBand2, CoarseDirt), If(clayBand3, CoarseDirt),
                    grassOrDirtIfUnderwater))),
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.SWAMP), If(swampPuddleLevel,
                    If(Not(aboveOverworldSeaLevel), If(Noise2D(Noises.Swamp, 0.0), Water)))),
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.MANGROVE_SWAMP), If(mangroveSwampPuddleLevel,
                    If(Not(aboveOverworldSeaLevel), If(Noise2D(Noises.Swamp, 0.0), Water)))))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.BADLANDS, NacreCraft.Registry.Biomes.ERODED_BADLANDS, NacreCraft.Registry.Biomes.WOODED_BADLANDS), Seq(
                If(OnFloor, Seq(
                    If(badlandsTop, OrangeTerracotta),
                    If(badlandsMid, Seq(If(clayBand1, Terracotta), If(clayBand2, Terracotta),
                        If(clayBand3, Terracotta), new SurfaceRules.Bandlands())),
                    If(notUnderwater, Seq(If(OnCeiling, RedSandstone), RedSand)),
                    If(Not(hole), OrangeTerracotta),
                    If(notUnderDeepWater, WhiteTerracotta),
                    gravelOrStoneIfCeiling)),
                If(badlandsHeightCondition, Seq(
                    If(aboveOverworldSeaLevel, If(Not(badlandsMid), OrangeTerracotta)),
                    new SurfaceRules.Bandlands())),
                If(UnderFloor, If(notUnderDeepWater, WhiteTerracotta)))),
            If(OnFloor, If(notUnderwater, Seq(
                If(frozenOcean, If(hole, Seq(If(aboveWater, Air),
                    If(new SurfaceRules.Temperature(), Ice), Water))),
                biomeSurfaceRule))),
            If(notUnderDeepWater, Seq(
                If(OnFloor, If(frozenOcean, If(hole, Water))),
                If(UnderFloor, biomeUnderSurfaceRule),
                If(biomesWithSandAndSandstone, If(DeepUnderFloor, Sandstone)),
                If(biomesWithSandAndVeryDeepSandstone, If(VeryDeepUnderFloor, Sandstone)))),
            If(OnFloor, Seq(
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.FROZEN_PEAKS, NacreCraft.Registry.Biomes.JAGGED_PEAKS), Stone),
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.WARM_OCEAN, NacreCraft.Registry.Biomes.LUKEWARM_OCEAN, NacreCraft.Registry.Biomes.DEEP_LUKEWARM_OCEAN),
                    sandOrSandstoneIfCeiling),
                gravelOrStoneIfCeiling)));
        var rules = new List<SurfaceRules.RuleSource>();
        if (bedrockRoof)
            rules.Add(If(Not(VerticalGradient("bedrock_roof", VerticalAnchor.BelowTop(5), VerticalAnchor.AboveBottom(0))),
                Bedrock));
        if (bedrockFloor)
            rules.Add(If(VerticalGradient("bedrock_floor", VerticalAnchor.AboveBottom(0), VerticalAnchor.AboveBottom(5)),
                Bedrock));
        var ruleAbovePreliminarySurface = If(new SurfaceRules.AbovePreliminarySurface(), mainRuleCloseToSurface);
        rules.Add(doPreliminarySurfaceCheck ? ruleAbovePreliminarySurface : mainRuleCloseToSurface);
        rules.Add(If(IsBiome(biomes, NacreCraft.Registry.Biomes.SULFUR_CAVES), sulfurCaveBands));
        rules.Add(If(VerticalGradient("deepslate", VerticalAnchor.Absolute(0), VerticalAnchor.Absolute(8)), Deepslate));
        return Seq(rules.ToArray());
    }

    public static SurfaceRules.RuleSource Nether(HolderLookup<Biome> biomes)
    {
        var aboveNetherLavaLevel = YBlock(VerticalAnchor.Absolute(31), 0);
        var aboveNetherLavaSurface = YBlock(VerticalAnchor.Absolute(32), 0);
        var netherBandAroundLavaLevelBottom = YStart(VerticalAnchor.Absolute(30), 0);
        var netherBandAroundLavaLevelTop = Not(YStart(VerticalAnchor.Absolute(35), 0));
        var closeToCeiling = YBlock(VerticalAnchor.BelowTop(5), 0);
        var hole = new SurfaceRules.Hole();
        var soulSandLayer = Noise2D(Noises.SoulSandLayer, -0.012);
        var gravelLayer = Noise2D(Noises.GravelLayer, -0.012);
        var patch = Noise2D(Noises.Patch, -0.012);
        var netherrack = Noise2D(Noises.Netherrack, 0.54);
        var netherWart = Noise2D(Noises.NetherWart, 1.17);
        var netherStateSelector = Noise2D(Noises.NetherStateSelector, 0.0);
        var gravelPatch = If(patch, If(netherBandAroundLavaLevelBottom,
            If(netherBandAroundLavaLevelTop, Gravel)));
        return Seq(
            If(VerticalGradient("bedrock_floor", VerticalAnchor.AboveBottom(0), VerticalAnchor.AboveBottom(5)),
                Bedrock),
            If(Not(VerticalGradient("bedrock_roof", VerticalAnchor.BelowTop(5), VerticalAnchor.AboveBottom(0))),
                Bedrock),
            If(closeToCeiling, Netherrack),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.BASALT_DELTAS), Seq(
                If(UnderCeiling, Basalt),
                If(UnderFloor, Seq(gravelPatch, If(netherStateSelector, Basalt), Blackstone)))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.SOUL_SAND_VALLEY), Seq(
                If(UnderCeiling, Seq(If(netherStateSelector, SoulSand), SoulSoil)),
                If(UnderFloor, Seq(gravelPatch, If(netherStateSelector, SoulSand), SoulSoil)))),
            If(OnFloor, Seq(
                If(Not(aboveNetherLavaSurface), If(hole, Lava)),
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.WARPED_FOREST), If(Not(netherrack),
                    If(aboveNetherLavaLevel, Seq(If(netherWart, WarpedWartBlock), WarpedNylium)))),
                If(IsBiome(biomes, NacreCraft.Registry.Biomes.CRIMSON_FOREST), If(Not(netherrack),
                    If(aboveNetherLavaLevel, Seq(If(netherWart, NetherWartBlock), CrimsonNylium)))))),
            If(IsBiome(biomes, NacreCraft.Registry.Biomes.NETHER_WASTES), Seq(
                If(UnderFloor, If(soulSandLayer, Seq(
                    If(Not(hole), If(netherBandAroundLavaLevelBottom,
                        If(netherBandAroundLavaLevelTop, SoulSand))),
                    Netherrack))),
                If(OnFloor, If(aboveNetherLavaLevel, If(netherBandAroundLavaLevelTop,
                    If(gravelLayer, Seq(If(aboveNetherLavaSurface, Gravel),
                        If(Not(hole), Gravel)))))))),
            Netherrack);
    }

    public static SurfaceRules.RuleSource End() => Endstone;

    public static SurfaceRules.RuleSource AirRule() => Air;
}
