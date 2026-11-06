using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Carver;
using NacreCraft.Registry;
//Both NacreCraft.Registry and the Game carver namespace name a ConfiguredWorldCarver; the registry key type is the marker one
using GameConfiguredWorldCarver = NacreCraft.Game.World.Level.LevelGen.Carver.ConfiguredWorldCarver;
using GameWorldCarver = NacreCraft.Game.World.Level.LevelGen.Carver.WorldCarver;
using RegistryConfiguredWorldCarver = NacreCraft.Registry.ConfiguredWorldCarver;

namespace NacreCraft.Game.Data.WorldGen;

//Carvers built-in configured carvers, maps to vanilla net.minecraft.data.worldgen.Carvers
//Four entries: the overworld cave, the deep overworld cave, the canyon and the nether cave
public static class Carvers
{
    //Cave the overworld cave carver, maps to vanilla CAVE
    public static readonly ResourceKey<RegistryConfiguredWorldCarver> Cave = CreateKey("cave");

    //CaveExtraUnderground the deeper overworld cave carver, maps to vanilla CAVE_EXTRA_UNDERGROUND
    public static readonly ResourceKey<RegistryConfiguredWorldCarver> CaveExtraUnderground = CreateKey("cave_extra_underground");

    //Canyon the overworld canyon carver, maps to vanilla CANYON
    public static readonly ResourceKey<RegistryConfiguredWorldCarver> Canyon = CreateKey("canyon");

    //NetherCave the nether cave carver, maps to vanilla NETHER_CAVE
    public static readonly ResourceKey<RegistryConfiguredWorldCarver> NetherCave = CreateKey("nether_cave");

    private static ResourceKey<RegistryConfiguredWorldCarver> CreateKey(string name)
        => ResourceKey<RegistryConfiguredWorldCarver>.Create(Registries.CONFIGURED_CARVER,
            Identifier.WithDefaultNamespace(name));

    public static void Bootstrap(BootstrapContext<RegistryConfiguredWorldCarver> context)
    {
        var blocks = context.LookupOrThrow(Registries.BLOCK);
        var overworldReplaceables = blocks.GetOrCreate(BlockTags.OverworldCarverReplaceables);
        var netherReplaceables = blocks.GetOrCreate(BlockTags.NetherCarverReplaceables);
        context.Register(Cave, new GameConfiguredWorldCarver(GameWorldCarver.CAVE, new CaveCarverConfiguration(
            0.15f, UniformHeight.Of(VerticalAnchor.AboveBottom(8), VerticalAnchor.Absolute(180)),
            UniformFloat.Of(0.1f, 0.9f), VerticalAnchor.AboveBottom(8), overworldReplaceables,
            UniformFloat.Of(0.7f, 1.4f), UniformFloat.Of(0.8f, 1.3f), UniformFloat.Of(-1.0f, -0.4f))));
        context.Register(CaveExtraUnderground, new GameConfiguredWorldCarver(GameWorldCarver.CAVE, new CaveCarverConfiguration(
            0.07f, UniformHeight.Of(VerticalAnchor.AboveBottom(8), VerticalAnchor.Absolute(47)),
            UniformFloat.Of(0.1f, 0.9f), VerticalAnchor.AboveBottom(8), overworldReplaceables,
            UniformFloat.Of(0.7f, 1.4f), UniformFloat.Of(0.8f, 1.3f), UniformFloat.Of(-1.0f, -0.4f))));
        context.Register(Canyon, new GameConfiguredWorldCarver(GameWorldCarver.CANYON, new CanyonCarverConfiguration(
            new CarverConfiguration(0.01f, UniformHeight.Of(VerticalAnchor.Absolute(10), VerticalAnchor.Absolute(67)),
                ConstantFloat.Of(3.0f), VerticalAnchor.AboveBottom(8), CarverDebugSettings.DEFAULT, overworldReplaceables),
            UniformFloat.Of(-0.125f, 0.125f),
            new CanyonShapeConfiguration(UniformFloat.Of(0.75f, 1.0f), TrapezoidFloat.Of(0.0f, 6.0f, 2.0f), 3,
                UniformFloat.Of(0.75f, 1.0f), 1.0f, 0.0f))));
        context.Register(NetherCave, new GameConfiguredWorldCarver(GameWorldCarver.NETHER_CAVE, new CaveCarverConfiguration(
            0.2f, UniformHeight.Of(VerticalAnchor.Absolute(0), VerticalAnchor.BelowTop(1)),
            ConstantFloat.Of(0.5f), VerticalAnchor.AboveBottom(10), netherReplaceables,
            ConstantFloat.Of(1.0f), ConstantFloat.Of(1.0f), ConstantFloat.Of(-0.7f))));
    }
}
