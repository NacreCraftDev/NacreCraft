namespace NacreCraft.Game.World.Entity;

//MoverType the source of a movement, maps to vanilla net.minecraft.world.entity.MoverType
public enum MoverType
{
    SELF,
    PLAYER,
    PISTON,
    SHULKER_BOX,
    SHULKER
}

//EntitySpawnReason why an entity is being created, maps to vanilla net.minecraft.world.entity.EntitySpawnReason
public enum EntitySpawnReason
{
    NATURAL,
    CHUNK_GENERATION,
    SPAWNER,
    STRUCTURE,
    BREEDING,
    MOB_SUMMONED,
    JOCKEY,
    EVENT,
    CONVERSION,
    REINFORCEMENT,
    TRIGGERED,
    BUCKET,
    SPAWN_ITEM_USE,
    COMMAND,
    DISPENSER,
    PATROL,
    TRIAL_SPAWNER,
    LOAD,
    DIMENSION_TRAVEL,
}

//EntitySpawnReasonExtensions spawn reason queries, maps to the static helpers of the vanilla enum
public static class EntitySpawnReasonExtensions
{
    //IsSpawner whether the reason is a spawner, maps to vanilla isSpawner
    public static bool IsSpawner(this EntitySpawnReason reason)
        => reason is EntitySpawnReason.SPAWNER or EntitySpawnReason.TRIAL_SPAWNER;

    //IgnoresLightRequirements whether the reason skips light checks, maps to vanilla ignoresLightRequirements
    public static bool IgnoresLightRequirements(this EntitySpawnReason reason)
        => reason == EntitySpawnReason.TRIAL_SPAWNER;
}

//Relative relative components of a teleport, maps to vanilla net.minecraft.world.entity.Relative
//Kept separate from the network RelativeFlag bitset: this enum drives the position/rotation math of teleports
public enum Relative
{
    X,
    Y,
    Z,
    Y_ROT,
    X_ROT,
    DELTA_X,
    DELTA_Y,
    DELTA_Z,
    ROTATE_DELTA,
}

//RelativeSets the named relative sets, maps to the static fields of vanilla Relative
public static class RelativeSets
{
    public static readonly IReadOnlySet<Relative> Direction = new HashSet<Relative>
    {
        Relative.DELTA_X, Relative.DELTA_Y, Relative.DELTA_Z, Relative.ROTATE_DELTA,
    };

    public static readonly IReadOnlySet<Relative> Rotation = new HashSet<Relative>
    {
        Relative.Y_ROT, Relative.X_ROT,
    };

    public static readonly IReadOnlySet<Relative> Position = new HashSet<Relative>
    {
        Relative.X, Relative.Y, Relative.Z,
    };
}

//EntityEvent entity animation/sound event ids, maps to vanilla net.minecraft.world.entity.EntityEvent
public static class EntityEvent
{
    public const byte JUMP = 1;
    public const byte KINETIC_HIT = 2;
    public const byte DEATH = 3;
    public const byte START_ATTACKING = 4;
    public const byte STOP_ATTACKING = 5;
    public const byte TAMING_FAILED = 6;
    public const byte TAMING_SUCCEEDED = 7;
    public const byte SHAKE_WETNESS = 8;
    public const byte USE_ITEM_COMPLETE = 9;
    public const byte EAT_GRASS = 10;
    public const byte OFFER_FLOWER = 11;
    public const byte LOVE_HEARTS = 12;
    public const byte VILLAGER_ANGRY = 13;
    public const byte VILLAGER_HAPPY = 14;
    public const byte WITCH_HAT_MAGIC = 15;
    public const byte ZOMBIE_CONVERTING = 16;
    public const byte FIREWORKS_EXPLODE = 17;
    public const byte IN_LOVE_HEARTS = 18;
    public const byte SQUID_ANIM_SYNCH = 19;
    public const byte SILVERFISH_MERGE_ANIM = 20;
    public const byte GUARDIAN_ATTACK_SOUND = 21;
    public const byte REDUCED_DEBUG_INFO = 22;
    public const byte FULL_DEBUG_INFO = 23;
    public const byte PERMISSION_LEVEL_ALL = 24;
    public const byte PERMISSION_LEVEL_MODERATORS = 25;
    public const byte PERMISSION_LEVEL_GAMEMASTERS = 26;
    public const byte PERMISSION_LEVEL_ADMINS = 27;
    public const byte PERMISSION_LEVEL_OWNERS = 28;
    public const byte FISHING_ROD_REEL_IN = 31;
    public const byte ARMORSTAND_WOBBLE = 32;
    public const byte STOP_OFFER_FLOWER = 34;
    public const byte PROTECTED_FROM_DEATH = 35;
    public const byte DOLPHIN_LOOKING_FOR_TREASURE = 38;
    public const byte RAVAGER_STUNNED = 39;
    public const byte TRUSTING_FAILED = 40;
    public const byte TRUSTING_SUCCEEDED = 41;
    public const byte VILLAGER_SWEAT = 42;
    public const byte FOX_EAT = 45;
    public const byte TELEPORT = 46;
    public const byte MAINHAND_BREAK = 47;
    public const byte OFFHAND_BREAK = 48;
    public const byte HEAD_BREAK = 49;
    public const byte CHEST_BREAK = 50;
    public const byte LEGS_BREAK = 51;
    public const byte FEET_BREAK = 52;
    public const byte HONEY_SLIDE = 53;
    public const byte HONEY_JUMP = 54;
    public const byte SWAP_HANDS = 55;
    public const byte CANCEL_SHAKE_WETNESS = 56;
    public const byte START_RAM = 58;
    public const byte END_RAM = 59;
    public const byte POOF = 60;
    public const byte TENDRILS_SHIVER = 61;
    public const byte SONIC_CHARGE = 62;
    public const byte SNIFFER_DIGGING_SOUND = 63;
    public const byte ARMADILLO_PEEK = 64;
    public const byte BODY_BREAK = 65;
    public const byte SHAKE = 66;
    public const byte DROWN_PARTICLES = 67;
    public const byte SADDLE_BREAK = 68;
    public const byte RAVAGER_ROARED = 69;
    public const byte TNT_PRIME = 70;
}
