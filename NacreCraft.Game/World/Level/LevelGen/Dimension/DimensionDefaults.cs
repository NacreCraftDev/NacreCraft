namespace NacreCraft.Game.World.Level.LevelGen.Dimension;

//DimensionDefaults default constants shared by the built-in dimensions, maps to vanilla net.minecraft.world.level.dimension.DimensionDefaults
//Height values are the 1.21 overworld/nether/end presets
public static class DimensionDefaults
{
    public const int CloudThickness = 4;
    public const int BlockLightTint = -10100;
    public const int NightVisionColor = -6710887;
    public const float TurtleEggHatchChance = 0.002f;

    public const int OverworldMinY = -64;
    public const int OverworldLevelHeight = 384;
    public const int OverworldGenerationHeight = 384;
    public const int OverworldLogicalHeight = 384;
    public const float OverworldCloudHeight = 192.33f;

    public const int NetherMinY = 0;
    public const int NetherLevelHeight = 256;
    public const int NetherGenerationHeight = 128;
    public const int NetherLogicalHeight = 128;

    public const int EndMinY = 0;
    public const int EndLevelHeight = 256;
    public const int EndGenerationHeight = 128;
    public const int EndLogicalHeight = 256;
    public const int EndIslandBaseY = 63;
}
