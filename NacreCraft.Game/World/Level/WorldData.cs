using NacreCraft.Registry.Flag;

namespace NacreCraft.Game.World.Level;

//DataPackConfig enabled and disabled data pack list, maps to vanilla net.minecraft.world.level.DataPackConfig
//The codec is not wired yet; level.dat carries these lists as plain strings
public sealed class DataPackConfig
{
    public static readonly DataPackConfig Default = new(new[] { "vanilla" }, Array.Empty<string>());

    public IReadOnlyList<string> Enabled { get; }

    public IReadOnlyList<string> Disabled { get; }

    public DataPackConfig(IReadOnlyList<string> enabled, IReadOnlyList<string> disabled)
    {
        Enabled = enabled.ToArray();
        Disabled = disabled.ToArray();
    }
}

//WorldDataConfiguration world data pack and feature flag configuration, maps to vanilla net.minecraft.world.level.WorldDataConfiguration
public sealed record WorldDataConfiguration(DataPackConfig DataPacks, FeatureFlagSet EnabledFeatures)
{
    public const string EnabledFeaturesId = "enabled_features";

    public static readonly WorldDataConfiguration Default = new(DataPackConfig.Default, FeatureFlags.DEFAULT_FLAGS);

    //ExpandFeatures joins the new feature flags into the current set, maps to vanilla expandFeatures
    public WorldDataConfiguration ExpandFeatures(FeatureFlagSet newEnabledFeatures)
        => new(DataPacks, EnabledFeatures.Join(newEnabledFeatures));
}

//LevelSettings immutable world creation settings, maps to vanilla net.minecraft.world.level.LevelSettings
//The Dynamic/Codec parsing entry point is omitted until the level.dat codec wiring lands
public sealed record LevelSettings
{
    public string LevelName { get; }

    public GameType GameType { get; }

    //DifficultySettingsData the difficulty settings, the member is not named DifficultySettings because the nested type owns that name
    public DifficultySettings DifficultySettingsData { get; }

    public bool AllowCommands { get; }

    public WorldDataConfiguration DataConfiguration { get; }

    public LevelSettings(string levelName, GameType gameType, DifficultySettings difficultySettings, bool allowCommands,
        WorldDataConfiguration dataConfiguration)
    {
        LevelName = levelName;
        GameType = gameType;
        DifficultySettingsData = difficultySettings;
        AllowCommands = allowCommands;
        DataConfiguration = dataConfiguration;
    }

    public LevelSettings WithGameType(GameType gameType)
        => new(LevelName, gameType, DifficultySettingsData, AllowCommands, DataConfiguration);

    public LevelSettings WithAllowCommands(bool allowCommands)
        => new(LevelName, GameType, DifficultySettingsData, allowCommands, DataConfiguration);

    public LevelSettings WithDifficulty(Difficulty difficulty)
        => new(LevelName, GameType,
            new DifficultySettings(difficulty, DifficultySettingsData.Hardcore, DifficultySettingsData.Locked),
            AllowCommands, DataConfiguration);

    public LevelSettings WithDifficultyLock(bool locked)
        => new(LevelName, GameType,
            new DifficultySettings(DifficultySettingsData.Difficulty, DifficultySettingsData.Hardcore, locked),
            AllowCommands, DataConfiguration);

    public LevelSettings WithDataConfiguration(WorldDataConfiguration dataConfiguration)
        => new(LevelName, GameType, DifficultySettingsData, AllowCommands, dataConfiguration);

    public LevelSettings Copy() => new(LevelName, GameType, DifficultySettingsData, AllowCommands, DataConfiguration);

    //DifficultySettings difficulty, hardcore and lock flags, maps to vanilla LevelSettings.DifficultySettings
    public sealed record DifficultySettings(Difficulty Difficulty, bool Hardcore, bool Locked)
    {
        public static readonly DifficultySettings Default =
            new(NacreCraft.Game.World.Level.Difficulty.Normal, false, false);
    }
}
