using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.GameEvents;

//GameEventTags game event tags, maps to vanilla net.minecraft.tags.GameEventTags
//Values come from datapack data/<ns>/tags/game_event/*.json and are bound to the registry by TagsReloadListener at startup
//Only the ones in use are registered here, the rest are added as needed
public static class GameEventTags
{
    //Vibrations events a vibration listener accepts, maps to vanilla VIBRATIONS
    //A sculk sensor and a warden both read this; anything outside it never becomes a candidate
    public static readonly TagKey<GameEvent> Vibrations = Create("vibrations");

    //IgnoreVibrationsSneaking events a sneaking player does not trigger, maps to vanilla IGNORE_VIBRATIONS_SNEAKING
    //Stepping carefully is meant to walk past a sensor without setting it off
    public static readonly TagKey<GameEvent> IgnoreVibrationsSneaking = Create("ignore_vibrations_sneaking");

    //AllayCanListen events an allay listens to, maps to vanilla ALLAY_CAN_LISTEN
    public static readonly TagKey<GameEvent> AllayCanListen = Create("allay_can_listen");

    //ShriekerCanListen events a sculk shrieker listens to, maps to vanilla SHRIEKER_CAN_LISTEN
    public static readonly TagKey<GameEvent> ShriekerCanListen = Create("shrieker_can_listen");

    //WardenCanListen events a warden listens to, maps to vanilla WARDEN_CAN_LISTEN
    public static readonly TagKey<GameEvent> WardenCanListen = Create("warden_can_listen");

    private static TagKey<GameEvent> Create(string path)
        => TagKey<GameEvent>.Create(Registries.GAME_EVENT, Identifier.WithDefaultNamespace(path));
}
