using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level;

//GameRules game rule definition table, maps to vanilla net.minecraft.world.level.gamerules.GameRules
//Names, types, defaults and value ranges align with the declaration order of vanilla 26.2 static blocks
//Runtime values are held by GameRuleMap; this class only provides definitions, registration and lookup by name
public static class GameRules
{
    public static readonly GameRule<object> AdvanceTime = Boolean("advance_time", GameRuleCategory.Updates, true);
    public static readonly GameRule<object> AdvanceWeather = Boolean("advance_weather", GameRuleCategory.Updates, true);
    public static readonly GameRule<object> AllowEnteringNetherUsingPortals = Boolean("allow_entering_nether_using_portals", GameRuleCategory.Misc, true);
    public static readonly GameRule<object> BlockDrops = Boolean("block_drops", GameRuleCategory.Drops, true);
    public static readonly GameRule<object> BlockExplosionDropDecay = Boolean("block_explosion_drop_decay", GameRuleCategory.Drops, true);
    public static readonly GameRule<object> CommandBlocksWork = Boolean("command_blocks_work", GameRuleCategory.Misc, true);
    public static readonly GameRule<object> CommandBlockOutput = Boolean("command_block_output", GameRuleCategory.Chat, true);
    public static readonly GameRule<object> DrowningDamage = Boolean("drowning_damage", GameRuleCategory.Player, true);
    public static readonly GameRule<object> ElytraMovementCheck = Boolean("elytra_movement_check", GameRuleCategory.Player, true);
    public static readonly GameRule<object> EnderPearlsVanishOnDeath = Boolean("ender_pearls_vanish_on_death", GameRuleCategory.Player, true);
    public static readonly GameRule<object> EntityDrops = Boolean("entity_drops", GameRuleCategory.Drops, true);
    public static readonly GameRule<object> FallDamage = Boolean("fall_damage", GameRuleCategory.Player, true);
    public static readonly GameRule<object> FireDamage = Boolean("fire_damage", GameRuleCategory.Player, true);
    public static readonly GameRule<object> FireSpreadRadiusAroundPlayer = Integer("fire_spread_radius_around_player", GameRuleCategory.Updates, 128, -1);
    public static readonly GameRule<object> ForgiveDeadPlayers = Boolean("forgive_dead_players", GameRuleCategory.Mobs, true);
    public static readonly GameRule<object> FreezeDamage = Boolean("freeze_damage", GameRuleCategory.Player, true);
    public static readonly GameRule<object> GlobalSoundEvents = Boolean("global_sound_events", GameRuleCategory.Misc, true);
    public static readonly GameRule<object> ImmediateRespawn = Boolean("immediate_respawn", GameRuleCategory.Player, false);
    public static readonly GameRule<object> KeepInventory = Boolean("keep_inventory", GameRuleCategory.Player, false);
    public static readonly GameRule<object> LavaSourceConversion = Boolean("lava_source_conversion", GameRuleCategory.Updates, false);
    public static readonly GameRule<object> LimitedCrafting = Boolean("limited_crafting", GameRuleCategory.Player, false);
    public static readonly GameRule<object> LocatorBar = Boolean("locator_bar", GameRuleCategory.Player, true);
    public static readonly GameRule<object> LogAdminCommands = Boolean("log_admin_commands", GameRuleCategory.Chat, true);
    public static readonly GameRule<object> MaxBlockModifications = Integer("max_block_modifications", GameRuleCategory.Misc, 32768, 1);
    public static readonly GameRule<object> MaxCommandForks = Integer("max_command_forks", GameRuleCategory.Misc, 65536, 0);
    public static readonly GameRule<object> MaxCommandSequenceLength = Integer("max_command_sequence_length", GameRuleCategory.Misc, 65536, 0);
    public static readonly GameRule<object> MaxEntityCramming = Integer("max_entity_cramming", GameRuleCategory.Mobs, 24, 0);
    public static readonly GameRule<object> MaxMinecartSpeed = Integer("max_minecart_speed", GameRuleCategory.Misc, 8, 1, 1000);
    public static readonly GameRule<object> MaxSnowAccumulationHeight = Integer("max_snow_accumulation_height", GameRuleCategory.Updates, 1, 0, 8);
    public static readonly GameRule<object> MobDrops = Boolean("mob_drops", GameRuleCategory.Drops, true);
    public static readonly GameRule<object> MobExplosionDropDecay = Boolean("mob_explosion_drop_decay", GameRuleCategory.Drops, true);
    public static readonly GameRule<object> MobGriefing = Boolean("mob_griefing", GameRuleCategory.Mobs, true);
    public static readonly GameRule<object> NaturalHealthRegeneration = Boolean("natural_health_regeneration", GameRuleCategory.Player, true);
    public static readonly GameRule<object> PlayerMovementCheck = Boolean("player_movement_check", GameRuleCategory.Player, true);
    public static readonly GameRule<object> PlayersNetherPortalCreativeDelay = Integer("players_nether_portal_creative_delay", GameRuleCategory.Player, 0, 0);
    public static readonly GameRule<object> PlayersNetherPortalDefaultDelay = Integer("players_nether_portal_default_delay", GameRuleCategory.Player, 80, 0);
    public static readonly GameRule<object> PlayersSleepingPercentage = Integer("players_sleeping_percentage", GameRuleCategory.Player, 100, 0);
    public static readonly GameRule<object> ProjectilesCanBreakBlocks = Boolean("projectiles_can_break_blocks", GameRuleCategory.Drops, true);
    public static readonly GameRule<object> Pvp = Boolean("pvp", GameRuleCategory.Player, true);
    public static readonly GameRule<object> Raids = Boolean("raids", GameRuleCategory.Mobs, true);
    public static readonly GameRule<object> RandomTickSpeed = Integer("random_tick_speed", GameRuleCategory.Updates, 3, 0);
    public static readonly GameRule<object> ReducedDebugInfo = Boolean("reduced_debug_info", GameRuleCategory.Misc, false);
    public static readonly GameRule<object> RespawnRadius = Integer("respawn_radius", GameRuleCategory.Player, 10, 0);
    public static readonly GameRule<object> SendCommandFeedback = Boolean("send_command_feedback", GameRuleCategory.Chat, true);
    public static readonly GameRule<object> ShowAdvancementMessages = Boolean("show_advancement_messages", GameRuleCategory.Chat, true);
    public static readonly GameRule<object> ShowDeathMessages = Boolean("show_death_messages", GameRuleCategory.Chat, true);
    public static readonly GameRule<object> SpawnerBlocksWork = Boolean("spawner_blocks_work", GameRuleCategory.Misc, true);
    public static readonly GameRule<object> SpawnMobs = Boolean("spawn_mobs", GameRuleCategory.Spawning, true);
    public static readonly GameRule<object> SpawnMonsters = Boolean("spawn_monsters", GameRuleCategory.Spawning, true);
    public static readonly GameRule<object> SpawnPatrols = Boolean("spawn_patrols", GameRuleCategory.Spawning, true);
    public static readonly GameRule<object> SpawnPhantoms = Boolean("spawn_phantoms", GameRuleCategory.Spawning, true);
    public static readonly GameRule<object> SpawnWanderingTraders = Boolean("spawn_wandering_traders", GameRuleCategory.Spawning, true);
    public static readonly GameRule<object> SpawnWardens = Boolean("spawn_wardens", GameRuleCategory.Spawning, true);
    public static readonly GameRule<object> SpectatorsGenerateChunks = Boolean("spectators_generate_chunks", GameRuleCategory.Player, true);
    public static readonly GameRule<object> SpreadVines = Boolean("spread_vines", GameRuleCategory.Updates, true);
    public static readonly GameRule<object> TntExplodes = Boolean("tnt_explodes", GameRuleCategory.Misc, true);
    public static readonly GameRule<object> TntExplosionDropDecay = Boolean("tnt_explosion_drop_decay", GameRuleCategory.Drops, false);
    public static readonly GameRule<object> UniversalAnger = Boolean("universal_anger", GameRuleCategory.Mobs, false);
    public static readonly GameRule<object> WaterSourceConversion = Boolean("water_source_conversion", GameRuleCategory.Updates, true);

    //All all rules in registration order, matching the vanilla static block order
    public static readonly GameRule<object>[] All =
    {
        AdvanceTime, AdvanceWeather, AllowEnteringNetherUsingPortals, BlockDrops, BlockExplosionDropDecay,
        CommandBlocksWork, CommandBlockOutput, DrowningDamage, ElytraMovementCheck, EnderPearlsVanishOnDeath,
        EntityDrops, FallDamage, FireDamage, FireSpreadRadiusAroundPlayer, ForgiveDeadPlayers,
        FreezeDamage, GlobalSoundEvents, ImmediateRespawn, KeepInventory, LavaSourceConversion,
        LimitedCrafting, LocatorBar, LogAdminCommands, MaxBlockModifications, MaxCommandForks,
        MaxCommandSequenceLength, MaxEntityCramming, MaxMinecartSpeed, MaxSnowAccumulationHeight, MobDrops,
        MobExplosionDropDecay, MobGriefing, NaturalHealthRegeneration, PlayerMovementCheck,
        PlayersNetherPortalCreativeDelay, PlayersNetherPortalDefaultDelay, PlayersSleepingPercentage,
        ProjectilesCanBreakBlocks, Pvp, Raids,
        RandomTickSpeed, ReducedDebugInfo, RespawnRadius, SendCommandFeedback, ShowAdvancementMessages,
        ShowDeathMessages, SpawnerBlocksWork, SpawnMobs, SpawnMonsters, SpawnPatrols,
        SpawnPhantoms, SpawnWanderingTraders, SpawnWardens, SpectatorsGenerateChunks, SpreadVines,
        TntExplodes, TntExplosionDropDecay, UniversalAnger, WaterSourceConversion,
    };

    //Bootstrap register all rules into BuiltInRegistries.GAME_RULE, must be called before the registry is frozen
    public static void Bootstrap()
    {
        foreach (var rule in All)
            Registry<GameRule<object>>.Register(BuiltInRegistries.GAME_RULE, rule.Id, rule);
    }

    //Find lookup by rule name, both short names and namespaced forms match, returns null if not found
    public static GameRule<object>? Find(string name)
    {
        var id = Identifier.TryParse(name.Contains(':') ? name : "minecraft:" + name);
        return id is null ? null : BuiltInRegistries.GAME_RULE.GetValue(id.Value);
    }

    //Boolean declare a boolean rule
    private static GameRule<object> Boolean(string path, GameRuleCategory category, bool defaultValue)
        => new(category, Identifier.WithDefaultNamespace(path), GameRuleType.Bool, defaultValue);

    //Integer declare an integer rule, uses int.MaxValue when unbounded, matching vanilla IntegerArgumentType.integer(min, max)
    private static GameRule<object> Integer(string path, GameRuleCategory category, int defaultValue, int min, int max = int.MaxValue)
        => new(category, Identifier.WithDefaultNamespace(path), GameRuleType.Int, defaultValue, min, max);
}
