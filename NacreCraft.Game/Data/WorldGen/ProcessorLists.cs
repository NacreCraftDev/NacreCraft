using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;
using NacreCraft.Registry.State;
using GameStructureProcessor = NacreCraft.Game.World.Level.LevelGen.Structure.StructureProcessor;
using RegBlock = NacreCraft.Registry.Block;

namespace NacreCraft.Game.Data.WorldGen;

//ProcessorLists built-in structure processor lists, maps to vanilla net.minecraft.data.worldgen.ProcessorLists
//Blocks resolve by registry name; the pane/wall property rules use local property instances matched by name
public static class ProcessorLists
{
    private static readonly BooleanProperty PaneNorth = new("north");
    private static readonly BooleanProperty PaneSouth = new("south");
    private static readonly BooleanProperty PaneEast = new("east");
    private static readonly BooleanProperty PaneWest = new("west");
    private static readonly BooleanProperty Lit = new("lit");

    public static readonly ResourceKey<Registry.StructureProcessorList> Empty = CreateKey("empty");
    public static readonly ResourceKey<Registry.StructureProcessorList> ZombiePlains = CreateKey("zombie_plains");
    public static readonly ResourceKey<Registry.StructureProcessorList> ZombieSavanna = CreateKey("zombie_savanna");
    public static readonly ResourceKey<Registry.StructureProcessorList> ZombieSnowy = CreateKey("zombie_snowy");
    public static readonly ResourceKey<Registry.StructureProcessorList> ZombieTaiga = CreateKey("zombie_taiga");
    public static readonly ResourceKey<Registry.StructureProcessorList> ZombieDesert = CreateKey("zombie_desert");
    public static readonly ResourceKey<Registry.StructureProcessorList> Mossify10Percent = CreateKey("mossify_10_percent");
    public static readonly ResourceKey<Registry.StructureProcessorList> Mossify20Percent = CreateKey("mossify_20_percent");
    public static readonly ResourceKey<Registry.StructureProcessorList> Mossify70Percent = CreateKey("mossify_70_percent");
    public static readonly ResourceKey<Registry.StructureProcessorList> StreetPlains = CreateKey("street_plains");
    public static readonly ResourceKey<Registry.StructureProcessorList> StreetSavanna = CreateKey("street_savanna");
    public static readonly ResourceKey<Registry.StructureProcessorList> StreetSnowyOrTaiga = CreateKey("street_snowy_or_taiga");
    public static readonly ResourceKey<Registry.StructureProcessorList> FarmPlains = CreateKey("farm_plains");
    public static readonly ResourceKey<Registry.StructureProcessorList> FarmSavanna = CreateKey("farm_savanna");
    public static readonly ResourceKey<Registry.StructureProcessorList> FarmSnowy = CreateKey("farm_snowy");
    public static readonly ResourceKey<Registry.StructureProcessorList> FarmTaiga = CreateKey("farm_taiga");
    public static readonly ResourceKey<Registry.StructureProcessorList> FarmDesert = CreateKey("farm_desert");
    public static readonly ResourceKey<Registry.StructureProcessorList> OutpostRot = CreateKey("outpost_rot");
    public static readonly ResourceKey<Registry.StructureProcessorList> BottomRampart = CreateKey("bottom_rampart");
    public static readonly ResourceKey<Registry.StructureProcessorList> TreasureRooms = CreateKey("treasure_rooms");
    public static readonly ResourceKey<Registry.StructureProcessorList> Housing = CreateKey("housing");
    public static readonly ResourceKey<Registry.StructureProcessorList> SideWallDegradation = CreateKey("side_wall_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> StableDegradation = CreateKey("stable_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> BastionGenericDegradation = CreateKey("bastion_generic_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> RampartDegradation = CreateKey("rampart_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> EntranceReplacement = CreateKey("entrance_replacement");
    public static readonly ResourceKey<Registry.StructureProcessorList> Bridge = CreateKey("bridge");
    public static readonly ResourceKey<Registry.StructureProcessorList> Roof = CreateKey("roof");
    public static readonly ResourceKey<Registry.StructureProcessorList> HighWall = CreateKey("high_wall");
    public static readonly ResourceKey<Registry.StructureProcessorList> HighRampart = CreateKey("high_rampart");
    public static readonly ResourceKey<Registry.StructureProcessorList> FossilRot = CreateKey("fossil_rot");
    public static readonly ResourceKey<Registry.StructureProcessorList> FossilCoal = CreateKey("fossil_coal");
    public static readonly ResourceKey<Registry.StructureProcessorList> FossilDiamonds = CreateKey("fossil_diamonds");
    public static readonly ResourceKey<Registry.StructureProcessorList> AncientCityStartDegradation =
        CreateKey("ancient_city_start_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> AncientCityGenericDegradation =
        CreateKey("ancient_city_generic_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> AncientCityWallsDegradation =
        CreateKey("ancient_city_walls_degradation");
    public static readonly ResourceKey<Registry.StructureProcessorList> TrailRuinsHousesArchaeology =
        CreateKey("trail_ruins_houses_archaeology");
    public static readonly ResourceKey<Registry.StructureProcessorList> TrailRuinsRoadsArchaeology =
        CreateKey("trail_ruins_roads_archaeology");
    public static readonly ResourceKey<Registry.StructureProcessorList> TrailRuinsTowerTopArchaeology =
        CreateKey("trail_ruins_tower_top_archaeology");
    public static readonly ResourceKey<Registry.StructureProcessorList> TrialChambersCopperBulbDegradation =
        CreateKey("trial_chambers_copper_bulb_degradation");

    private static ResourceKey<Registry.StructureProcessorList> CreateKey(string name)
        => ResourceKey<Registry.StructureProcessorList>.Create(Registries.PROCESSOR_LIST,
            Identifier.WithDefaultNamespace(name));

    public static void Bootstrap(BootstrapContext<Registry.StructureProcessorList> context)
    {
        var featuresCannotReplace = BuiltInRegistries.BLOCK.GetOrCreate(BlockTags.FeaturesCannotReplace);
        var ancientCityReplaceable = BuiltInRegistries.BLOCK.GetOrCreate(BlockTags.AncientCityReplaceable);
        var addGildedBlackstone = Rule(new RandomBlockMatchTest(B("blackstone"), 0.01f), S("gilded_blackstone"));
        var removeGildedBlackstone = Rule(new RandomBlockMatchTest(B("gilded_blackstone"), 0.5f), S("blackstone"));
        Register(context, Empty, Array.Empty<GameStructureProcessor>());
        Register(context, ZombiePlains, Processor(
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.8f), S("mossy_cobblestone")),
            Rule(new TagMatchTest(BlockTags.Doors), S("air")),
            Rule(new BlockMatchTest(B("torch")), S("air")),
            Rule(new BlockMatchTest(B("wall_torch")), S("air")),
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.07f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("mossy_cobblestone"), 0.07f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("white_terracotta"), 0.07f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("oak_log"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("oak_planks"), 0.1f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("oak_stairs"), 0.1f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("stripped_oak_log"), 0.02f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("glass_pane"), 0.5f), S("cobweb")),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneNorth, PaneSouth)),
                PaneState("brown_stained_glass_pane", PaneNorth, PaneSouth)),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneEast, PaneWest)),
                PaneState("brown_stained_glass_pane", PaneEast, PaneWest)),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.3f), S("carrots")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.2f), S("potatoes")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("beetroots"))));
        Register(context, ZombieSavanna, Processor(
            Rule(new TagMatchTest(BlockTags.Doors), S("air")),
            Rule(new BlockMatchTest(B("torch")), S("air")),
            Rule(new BlockMatchTest(B("wall_torch")), S("air")),
            Rule(new RandomBlockMatchTest(B("acacia_planks"), 0.2f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("acacia_stairs"), 0.2f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("acacia_log"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("acacia_wood"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("orange_terracotta"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("yellow_terracotta"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("red_terracotta"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("glass_pane"), 0.5f), S("cobweb")),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneNorth, PaneSouth)),
                PaneState("brown_stained_glass_pane", PaneNorth, PaneSouth)),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneEast, PaneWest)),
                PaneState("brown_stained_glass_pane", PaneEast, PaneWest)),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("melon_stem"))));
        Register(context, ZombieSnowy, Processor(
            Rule(new TagMatchTest(BlockTags.Doors), S("air")),
            Rule(new BlockMatchTest(B("torch")), S("air")),
            Rule(new BlockMatchTest(B("wall_torch")), S("air")),
            Rule(new BlockMatchTest(B("lantern")), S("air")),
            Rule(new RandomBlockMatchTest(B("spruce_planks"), 0.2f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("spruce_slab"), 0.4f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("stripped_spruce_log"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("stripped_spruce_wood"), 0.05f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("glass_pane"), 0.5f), S("cobweb")),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneNorth, PaneSouth)),
                PaneState("brown_stained_glass_pane", PaneNorth, PaneSouth)),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneEast, PaneWest)),
                PaneState("brown_stained_glass_pane", PaneEast, PaneWest)),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("carrots")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.8f), S("potatoes"))));
        Register(context, ZombieTaiga, Processor(
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.8f), S("mossy_cobblestone")),
            Rule(new TagMatchTest(BlockTags.Doors), S("air")),
            Rule(new BlockMatchTest(B("torch")), S("air")),
            Rule(new BlockMatchTest(B("wall_torch")), S("air")),
            Rule(new BlockMatchTest(B("campfire")), S("campfire").TrySetValue(Lit, false)),
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.08f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("spruce_log"), 0.08f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("glass_pane"), 0.5f), S("cobweb")),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneNorth, PaneSouth)),
                PaneState("brown_stained_glass_pane", PaneNorth, PaneSouth)),
            Rule(new BlockStateMatchTest(PaneState("glass_pane", PaneEast, PaneWest)),
                PaneState("brown_stained_glass_pane", PaneEast, PaneWest)),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.3f), S("pumpkin_stem")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.2f), S("potatoes"))));
        Register(context, ZombieDesert, Processor(
            Rule(new TagMatchTest(BlockTags.Doors), S("air")),
            Rule(new BlockMatchTest(B("torch")), S("air")),
            Rule(new BlockMatchTest(B("wall_torch")), S("air")),
            Rule(new RandomBlockMatchTest(B("smooth_sandstone"), 0.08f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("cut_sandstone"), 0.1f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("terracotta"), 0.08f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("smooth_sandstone_stairs"), 0.08f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("smooth_sandstone_slab"), 0.08f), S("cobweb")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.2f), S("beetroots")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("melon_stem"))));
        Register(context, Mossify10Percent, Processor(
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.1f), S("mossy_cobblestone"))));
        Register(context, Mossify20Percent, Processor(
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.2f), S("mossy_cobblestone"))));
        Register(context, Mossify70Percent, Processor(
            Rule(new RandomBlockMatchTest(B("cobblestone"), 0.7f), S("mossy_cobblestone"))));
        Register(context, StreetPlains, Processor(
            LocationRule(B("dirt_path"), B("water"), S("oak_planks")),
            Rule(new RandomBlockMatchTest(B("dirt_path"), 0.1f), S("grass_block")),
            LocationRule(B("grass_block"), B("water"), S("water")),
            LocationRule(B("dirt"), B("water"), S("water"))));
        Register(context, StreetSavanna, Processor(
            LocationRule(B("dirt_path"), B("water"), S("acacia_planks")),
            Rule(new RandomBlockMatchTest(B("dirt_path"), 0.2f), S("grass_block")),
            LocationRule(B("grass_block"), B("water"), S("water")),
            LocationRule(B("dirt"), B("water"), S("water"))));
        Register(context, StreetSnowyOrTaiga, Processor(
            LocationRule(B("dirt_path"), B("water"), S("spruce_planks")),
            LocationRule(B("dirt_path"), B("ice"), S("spruce_planks")),
            Rule(new RandomBlockMatchTest(B("dirt_path"), 0.2f), S("grass_block")),
            LocationRule(B("grass_block"), B("water"), S("water")),
            LocationRule(B("dirt"), B("water"), S("water"))));
        Register(context, FarmPlains, Processor(
            Rule(new RandomBlockMatchTest(B("wheat"), 0.3f), S("carrots")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.2f), S("potatoes")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("beetroots"))));
        Register(context, FarmSavanna, Processor(
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("melon_stem"))));
        Register(context, FarmSnowy, Processor(
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("carrots")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.8f), S("potatoes"))));
        Register(context, FarmTaiga, Processor(
            Rule(new RandomBlockMatchTest(B("wheat"), 0.3f), S("pumpkin_stem")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.2f), S("potatoes"))));
        Register(context, FarmDesert, Processor(
            Rule(new RandomBlockMatchTest(B("wheat"), 0.2f), S("beetroots")),
            Rule(new RandomBlockMatchTest(B("wheat"), 0.1f), S("melon_stem"))));
        Register(context, OutpostRot, new BlockRotProcessor(0.05f));
        Register(context, BottomRampart, Processor(
            Rule(new RandomBlockMatchTest(B("magma_block"), 0.75f), S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("cracked_polished_blackstone_bricks"), 0.15f),
                S("polished_blackstone_bricks")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, TreasureRooms, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.35f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("chiseled_polished_blackstone"), 0.1f),
                S("cracked_polished_blackstone_bricks")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, Housing, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.3f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("blackstone"), 1.0E-4f), S("air")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, SideWallDegradation, Processor(
            Rule(new RandomBlockMatchTest(B("chiseled_polished_blackstone"), 0.5f), S("air")),
            Rule(new RandomBlockMatchTest(B("gold_block"), 0.1f), S("cracked_polished_blackstone_bricks")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, StableDegradation, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.1f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("blackstone"), 1.0E-4f), S("air")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, BastionGenericDegradation, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.3f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("blackstone"), 1.0E-4f), S("air")),
            Rule(new RandomBlockMatchTest(B("gold_block"), 0.3f), S("cracked_polished_blackstone_bricks")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, RampartDegradation, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.4f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("blackstone"), 0.01f), S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 1.0E-4f), S("air")),
            Rule(new RandomBlockMatchTest(B("blackstone"), 1.0E-4f), S("air")),
            Rule(new RandomBlockMatchTest(B("gold_block"), 0.3f), S("cracked_polished_blackstone_bricks")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, EntranceReplacement, Processor(
            Rule(new RandomBlockMatchTest(B("chiseled_polished_blackstone"), 0.5f), S("air")),
            Rule(new RandomBlockMatchTest(B("gold_block"), 0.6f), S("cracked_polished_blackstone_bricks")),
            removeGildedBlackstone, addGildedBlackstone));
        Register(context, Bridge, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.3f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("blackstone"), 1.0E-4f), S("air"))));
        Register(context, Roof, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.3f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.15f), S("air")),
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.3f), S("blackstone"))));
        Register(context, HighWall, Processor(
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.01f), S("air")),
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.5f),
                S("cracked_polished_blackstone_bricks")),
            Rule(new RandomBlockMatchTest(B("polished_blackstone_bricks"), 0.3f), S("blackstone")),
            removeGildedBlackstone));
        Register(context, HighRampart, Processor(
            Rule(new RandomBlockMatchTest(B("gold_block"), 0.3f), S("cracked_polished_blackstone_bricks")),
            new ProcessorRule(AlwaysTrueTest.Instance, AlwaysTrueTest.Instance,
                new AxisAlignedLinearPosTest(0.0f, 0.05f, 0, 100, NacreCraft.Primitives.Direction.Axis.Y), S("air")),
            removeGildedBlackstone));
        Register(context, FossilRot, new BlockRotProcessor(0.9f), new ProtectedBlockProcessor(featuresCannotReplace));
        Register(context, FossilCoal, new BlockRotProcessor(0.1f),
            new ProtectedBlockProcessor(featuresCannotReplace));
        Register(context, FossilDiamonds, new BlockRotProcessor(0.1f), Processor(
                Rule(new BlockMatchTest(B("coal_ore")), S("deepslate_diamond_ore"))),
            new ProtectedBlockProcessor(featuresCannotReplace));
        Register(context, AncientCityStartDegradation, Processor(
                Rule(new RandomBlockMatchTest(B("deepslate_bricks"), 0.3f), S("cracked_deepslate_bricks")),
                Rule(new RandomBlockMatchTest(B("deepslate_tiles"), 0.3f), S("cracked_deepslate_tiles")),
                Rule(new RandomBlockMatchTest(B("soul_lantern"), 0.05f), S("air"))),
            new ProtectedBlockProcessor(featuresCannotReplace));
        Register(context, AncientCityGenericDegradation, new BlockRotProcessor(ancientCityReplaceable, 0.95f),
            Processor(
                Rule(new RandomBlockMatchTest(B("deepslate_bricks"), 0.3f), S("cracked_deepslate_bricks")),
                Rule(new RandomBlockMatchTest(B("deepslate_tiles"), 0.3f), S("cracked_deepslate_tiles")),
                Rule(new RandomBlockMatchTest(B("soul_lantern"), 0.05f), S("air"))),
            new ProtectedBlockProcessor(featuresCannotReplace));
        Register(context, AncientCityWallsDegradation, new BlockRotProcessor(ancientCityReplaceable, 0.95f),
            Processor(
                Rule(new RandomBlockMatchTest(B("deepslate_bricks"), 0.3f), S("cracked_deepslate_bricks")),
                Rule(new RandomBlockMatchTest(B("deepslate_tiles"), 0.3f), S("cracked_deepslate_tiles")),
                Rule(new RandomBlockMatchTest(B("deepslate_tile_slab"), 0.3f), S("air")),
                Rule(new RandomBlockMatchTest(B("soul_lantern"), 0.05f), S("air"))),
            new ProtectedBlockProcessor(featuresCannotReplace));
        Register(context, TrailRuinsHousesArchaeology, Processor(
                Rule(new RandomBlockMatchTest(B("gravel"), 0.2f), S("dirt")),
                Rule(new RandomBlockMatchTest(B("gravel"), 0.1f), S("coarse_dirt")),
                Rule(new RandomBlockMatchTest(B("mud_bricks"), 0.1f), S("packed_mud"))),
            TrailsArchyLootProcessor("archaeology/trail_ruins_common", 6),
            TrailsArchyLootProcessor("archaeology/trail_ruins_rare", 3));
        Register(context, TrailRuinsRoadsArchaeology, Processor(
                Rule(new RandomBlockMatchTest(B("gravel"), 0.2f), S("dirt")),
                Rule(new RandomBlockMatchTest(B("gravel"), 0.1f), S("coarse_dirt")),
                Rule(new RandomBlockMatchTest(B("mud_bricks"), 0.1f), S("packed_mud"))),
            TrailsArchyLootProcessor("archaeology/trail_ruins_common", 2));
        Register(context, TrailRuinsTowerTopArchaeology,
            TrailsArchyLootProcessor("archaeology/trail_ruins_common", 2));
        Register(context, TrialChambersCopperBulbDegradation, Processor(
                Rule(new RandomBlockMatchTest(B("waxed_copper_bulb"), 0.1f),
                    S("waxed_oxidized_copper_bulb").TrySetValue(Lit, true)),
                Rule(new RandomBlockMatchTest(B("waxed_copper_bulb"), 0.33333334f),
                    S("waxed_weathered_copper_bulb").TrySetValue(Lit, true)),
                Rule(new RandomBlockMatchTest(B("waxed_copper_bulb"), 0.5f),
                    S("waxed_exposed_copper_bulb").TrySetValue(Lit, true))),
            new ProtectedBlockProcessor(featuresCannotReplace));
    }

    //Block registry lookup by path
    private static RegBlock B(string path) => WorldGenDataSupport.Block(path);

    //State registry lookup by path
    private static BlockState S(string path) => WorldGenDataSupport.State(path);

    //Rule a rule with only the input predicate, maps to the two-argument ProcessorRule form
    private static ProcessorRule Rule(RuleTest input, BlockState output)
        => new(input, AlwaysTrueTest.Instance, output);

    //LocationRule a rule that also requires the world block, maps to the three-argument ProcessorRule form
    private static ProcessorRule LocationRule(RegBlock input, RegBlock location, BlockState output)
        => new(new BlockMatchTest(input), new BlockMatchTest(location), output);

    private static RuleProcessor Processor(params ProcessorRule[] rules) => new(rules);

    //PaneState a glass pane state with two of its four sides set, maps to the two-setValue chains in vanilla
    private static BlockState PaneState(string block, BooleanProperty first, BooleanProperty second)
        => S(block).TrySetValue(first, true).TrySetValue(second, true);

    //TrailsArchyLootProcessor the suspicious gravel loot processor, maps to vanilla trailsArchyLootProcessor
    private static CappedProcessor TrailsArchyLootProcessor(string lootTable, int count)
        => new(new RuleProcessor(new[]
        {
            new ProcessorRule(new TagMatchTest(BlockTags.TrailRuinsReplaceable), AlwaysTrueTest.Instance,
                PosAlwaysTrueTest.Instance, S("suspicious_gravel"),
                new AppendLootModifier(Identifier.WithDefaultNamespace(lootTable)))
        }), ConstantInt.Of(count));

    private static void Register(BootstrapContext<Registry.StructureProcessorList> context,
        ResourceKey<Registry.StructureProcessorList> key, params GameStructureProcessor[] processors)
        => context.Register(key, new NacreCraft.Game.World.Level.LevelGen.Structure.StructureProcessorList(processors));
}
