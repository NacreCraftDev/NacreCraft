using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//EntityTypes built-in entity type constants, maps to vanilla net.minecraft.world.entity.EntityTypes
//Registered into the BuiltInRegistries.ENTITY_TYPE registry
//A simplified version with only a few sample entities (pig/cow/chicken/zombie/player) to validate the registration framework; vanilla has 158 types and NC extends as needed
//RawId taken from the registration order in vanilla EntityTypes.java; the client resolves AddEntity packets by this index so it cannot be renumbered
//Note BuiltInRegistries.ENTITY_TYPE is a weakly typed EntityType<object> registry
//The object type parameter carries different concrete Entity subclasses, matching vanilla's type erasure
public static class EntityTypes
{
    //PigType pig type
    public sealed class PigType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("pig");
        public override int RawId => 100;
    }

    //CowType cow type
    public sealed class CowType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("cow");
        public override int RawId => 30;
    }

    //ChickenType chicken type
    public sealed class ChickenType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("chicken");
        public override int RawId => 26;
    }

    //ZombieType zombie type
    public sealed class ZombieType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("zombie");
        public override int RawId => 151;
    }

    //LightningBoltType lightning bolt type, maps to vanilla EntityTypes.LIGHTNING_BOLT
    //A zero sized entity with the largest update interval, it only carries the channeling strike
    public sealed class LightningBoltType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("lightning_bolt");
        public override int RawId => 77;
        public override int TrackingRangeChunks => 16;
        public override float Width => 0f;
        public override float Height => 0f;
    }

    //TridentType thrown trident type, maps to vanilla EntityTypes.TRIDENT
    public sealed class TridentType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("trident");
        public override int RawId => 136;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.5f;
        public override float Height => 0.5f;
    }

    //EvokerFangsType evoker fangs type, maps to vanilla EntityTypes.EVOKER_FANGS
    public sealed class EvokerFangsType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("evoker_fangs");
        public override int RawId => 47;
        public override int TrackingRangeChunks => 6;
        public override float Width => 0.5f;
        public override float Height => 0.8f;
    }

    //EyeOfEnderType eye of ender type, maps to vanilla EntityTypes.EYE_OF_ENDER
    public sealed class EyeOfEnderType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("eye_of_ender");
        public override int RawId => 50;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //FireworkRocketType firework rocket type, maps to vanilla EntityTypes.FIREWORK_ROCKET
    public sealed class FireworkRocketType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("firework_rocket");
        public override int RawId => 53;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //LlamaSpitType llama spit type, maps to vanilla EntityTypes.LLAMA_SPIT
    public sealed class LlamaSpitType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("llama_spit");
        public override int RawId => 79;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //ShulkerBulletType shulker bullet type, maps to vanilla EntityTypes.SHULKER_BULLET
    public sealed class ShulkerBulletType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("shulker_bullet");
        public override int RawId => 113;
        public override int TrackingRangeChunks => 8;
        public override float Width => 0.3125f;
        public override float Height => 0.3125f;
    }

    //FishingBobberType fishing bobber type, maps to vanilla EntityTypes.FISHING_BOBBER
    public sealed class FishingBobberType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("fishing_bobber");
        public override int RawId => 157;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //SpectralArrowType spectral arrow type, maps to vanilla EntityTypes.SPECTRAL_ARROW
    public sealed class SpectralArrowType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("spectral_arrow");
        public override int RawId => 123;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.5f;
        public override float Height => 0.5f;
    }

    //FireballType large fireball type, maps to vanilla EntityTypes.FIREBALL
    public sealed class FireballType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("fireball");
        public override int RawId => 52;
        public override int TrackingRangeChunks => 4;
        public override float Width => 1f;
        public override float Height => 1f;
    }

    //DragonFireballType dragon fireball type, maps to vanilla EntityTypes.DRAGON_FIREBALL
    public sealed class DragonFireballType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("dragon_fireball");
        public override int RawId => 37;
        public override int TrackingRangeChunks => 4;
        public override float Width => 1f;
        public override float Height => 1f;
    }

    //WitherSkullType wither skull type, maps to vanilla EntityTypes.WITHER_SKULL
    public sealed class WitherSkullType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("wither_skull");
        public override int RawId => 148;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.3125f;
        public override float Height => 0.3125f;
    }

    //ExperienceBottleType experience bottle type, maps to vanilla EntityTypes.EXPERIENCE_BOTTLE
    public sealed class ExperienceBottleType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("experience_bottle");
        public override int RawId => 48;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //SplashPotionType splash potion type, maps to vanilla EntityTypes.SPLASH_POTION
    public sealed class SplashPotionType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("splash_potion");
        public override int RawId => 105;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //LingeringPotionType lingering potion type, maps to vanilla EntityTypes.LINGERING_POTION
    public sealed class LingeringPotionType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("lingering_potion");
        public override int RawId => 106;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //PlayerType player type, used for player tracking and AddEntity broadcast
    public sealed class PlayerType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("player");
        public override int RawId => 156;
        //Players have the largest tracking range, maps to vanilla clientTrackingRange(32)
        public override int TrackingRangeChunks => 32;
    }

    //ItemType item entity type, maps to the clientTrackingRange(6) of vanilla EntityTypes.ITEM
    public sealed class ItemType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("item");
        public override int RawId => 71;
        //Item entities are tracked at a shorter range than mobs, maps to vanilla clientTrackingRange(6)
        public override int TrackingRangeChunks => 6;
        //Item entity collision box 0.25 square, maps to vanilla sized(0.25f, 0.25f)
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //ArrowType arrow type, maps to vanilla EntityTypes.ARROW
    public sealed class ArrowType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("arrow");
        public override int RawId => 6;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.5f;
        public override float Height => 0.5f;
    }

    //EggType egg type, maps to vanilla EntityTypes.EGG
    public sealed class EggType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("egg");
        public override int RawId => 39;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //EnderPearlType ender pearl type, maps to vanilla EntityTypes.ENDER_PEARL
    public sealed class EnderPearlType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("ender_pearl");
        public override int RawId => 44;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //SmallFireballType small fireball type, a thrown fire charge becomes this, maps to vanilla EntityTypes.SMALL_FIREBALL
    public sealed class SmallFireballType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("small_fireball");
        public override int RawId => 118;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.3125f;
        public override float Height => 0.3125f;
    }

    //SnowballType snowball type, maps to vanilla EntityTypes.SNOWBALL
    public sealed class SnowballType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("snowball");
        public override int RawId => 120;
        public override int TrackingRangeChunks => 4;
        public override float Width => 0.25f;
        public override float Height => 0.25f;
    }

    //SilverfishType silverfish, maps to vanilla EntityTypes.SILVERFISH
    //The silverfish mob class is not ported, so the type carries no factory and spawning degrades to a null return
    public sealed class SilverfishType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("silverfish");
        public override int RawId => 114;
        //Silverfish are tracked at eight chunks, maps to vanilla clientTrackingRange(8)
        public override int TrackingRangeChunks => 8;
        //Silverfish collision box 0.4 x 0.3, maps to vanilla sized(0.4f, 0.3f)
        public override float Width => 0.4f;
        public override float Height => 0.3f;
    }

    //SlimeType slime, maps to vanilla EntityTypes.SLIME
    //The slime mob class is not ported, so the type carries no factory and spawning degrades to a null return
    public sealed class SlimeType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("slime");
        public override int RawId => 117;
        //Slimes are tracked at ten chunks, maps to vanilla clientTrackingRange(10)
        public override int TrackingRangeChunks => 10;
        //Small slime box 0.52 square, maps to vanilla sized(0.52f, 0.52f)
        public override float Width => 0.52f;
        public override float Height => 0.52f;
    }

    //TntType primed TNT, maps to vanilla EntityTypes.TNT
    public sealed class TntType : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("tnt");
        public override int RawId => 133;
        //TNT is tracked at ten chunks, maps to vanilla clientTrackingRange(10)
        public override int TrackingRangeChunks => 10;
        //TNT collision box 0.98 square, maps to vanilla sized(0.98f, 0.98f)
        public override float Width => 0.98f;
        public override float Height => 0.98f;
    }

    public static readonly PigType PIG = new() { Factory = (type, level) => new Mob(type) };
    public static readonly CowType COW = new() { Factory = (type, level) => new Mob(type) };
    public static readonly ChickenType CHICKEN = new() { Factory = (type, level) => new Mob(type) };
    public static readonly ZombieType ZOMBIE = new() { Factory = (type, level) => new Mob(type) };
    public static readonly ItemType ITEM = new() { Factory = (type, level) => new ItemEntity(type) };

    //The five projectile types differ only in their launch position function, behavior is decided by each entity class
    public static readonly ArrowType ARROW = new() { Factory = (type, level) => new Arrow(type) };
    public static readonly EggType EGG = new() { Factory = (type, level) => new ThrownEgg(type) };
    public static readonly EnderPearlType ENDER_PEARL = new() { Factory = (type, level) => new ThrownEnderpearl(type) };
    public static readonly SnowballType SNOWBALL = new() { Factory = (type, level) => new Snowball(type) };
    public static readonly SmallFireballType SMALL_FIREBALL = new() { Factory = (type, level) => new SmallFireball(type) };

    //TNT primed TNT, spawned by igniting a TNT block
    public static readonly TntType TNT = new() { Factory = (type, level) => new PrimedTnt(type) };

    //Player entities go through playerdata and are not stored as entities, so no factory is given and they are skipped when restoring from entity saves
    public static readonly PlayerType PLAYER = new();

    //LIGHTNING_BOLT lightning bolt type, spawned by the channeling enchantment
    public static readonly LightningBoltType LIGHTNING_BOLT = new();

    //TRIDENT thrown trident type, the channeling entity predicate checks for it
    public static readonly TridentType TRIDENT = new();

    //SILVERFISH silverfish type, spawned by the infested effect; the mob class is not ported, so no factory is given
    public static readonly SilverfishType SILVERFISH = new();

    //SLIME slime type, spawned by the oozing effect; the mob class is not ported, so no factory is given
    public static readonly SlimeType SLIME = new();

    //Projectile entity types ported with the projectile batch
    public static readonly EvokerFangsType EVOKER_FANGS = new() { Factory = (type, level) => new EvokerFangs(type) };
    public static readonly EyeOfEnderType EYE_OF_ENDER = new() { Factory = (type, level) => new EyeOfEnder(type) };
    public static readonly FireworkRocketType FIREWORK_ROCKET = new() { Factory = (type, level) => new FireworkRocketEntity(type) };
    public static readonly LlamaSpitType LLAMA_SPIT = new() { Factory = (type, level) => new LlamaSpit(type) };
    public static readonly ShulkerBulletType SHULKER_BULLET = new() { Factory = (type, level) => new ShulkerBullet(type) };
    public static readonly FishingBobberType FISHING_BOBBER = new() { Factory = (type, level) => new FishingHook(type) };

    //Projectile subpackage types: spectral arrow, fireballs, wither skull and potion throwables
    public static readonly SpectralArrowType SPECTRAL_ARROW = new() { Factory = (type, level) => new SpectralArrow(type) };
    public static readonly FireballType FIREBALL = new() { Factory = (type, level) => new LargeFireball(type) };
    public static readonly DragonFireballType DRAGON_FIREBALL = new() { Factory = (type, level) => new DragonFireball(type) };
    public static readonly WitherSkullType WITHER_SKULL = new() { Factory = (type, level) => new WitherSkull(type) };
    public static readonly ExperienceBottleType EXPERIENCE_BOTTLE = new() { Factory = (type, level) => new ThrownExperienceBottle(type) };
    public static readonly SplashPotionType SPLASH_POTION = new() { Factory = (type, level) => new ThrownSplashPotion(type) };
    public static readonly LingeringPotionType LINGERING_POTION = new() { Factory = (type, level) => new ThrownLingeringPotion(type) };

    //_byRawId reverse map from network index to type, used by AddEntity decoding to resolve the entity type
    private static readonly Dictionary<int, EntityType<object>> ByRawId = new();

    //Bootstrap registers all built-in entity types into BuiltInRegistries.ENTITY_TYPE
    //Called by the Game layer Bootstrap after BuiltInRegistries.BootStrap
    public static void Bootstrap()
    {
        Register(PIG);
        Register(COW);
        Register(CHICKEN);
        Register(ZOMBIE);
        Register(ITEM);
        Register(ARROW);
        Register(EGG);
        Register(ENDER_PEARL);
        Register(SNOWBALL);
        Register(SMALL_FIREBALL);
        Register(TNT);
        Register(PLAYER);
        Register(LIGHTNING_BOLT);
        Register(TRIDENT);
        Register(SILVERFISH);
        Register(SLIME);
        Register(EVOKER_FANGS);
        Register(EYE_OF_ENDER);
        Register(FIREWORK_ROCKET);
        Register(LLAMA_SPIT);
        Register(SHULKER_BULLET);
        Register(FISHING_BOBBER);
        Register(SPECTRAL_ARROW);
        Register(FIREBALL);
        Register(DRAGON_FIREBALL);
        Register(WITHER_SKULL);
        Register(EXPERIENCE_BOTTLE);
        Register(SPLASH_POTION);
        Register(LINGERING_POTION);
    }

    //ById resolves an entity type by network index, null when unregistered
    public static EntityType<object>? ById(int rawId)
        => ByRawId.TryGetValue(rawId, out var type) ? type : null;

    //UnknownType placeholder type for unregistered network indices, the client keeps the entity but does not render it
    //The vanilla client also keeps the entity for unknown types, just without a model; throwing during decode would drop the whole packet
    private sealed class UnknownType(int rawId) : EntityType<object>
    {
        public override Identifier Id => Identifier.WithDefaultNamespace("unknown");
        public override int RawId { get; } = rawId;
    }

    //ByIdOrUnknown resolves an entity type by network index, an unregistered one gives a placeholder preserving the original index
    public static EntityType<object> ByIdOrUnknown(int rawId)
        => ByRawId.TryGetValue(rawId, out var type) ? type : new UnknownType(rawId);

    //Register registers an entity type into the ENTITY_TYPE registry and records its network index
    private static void Register(EntityType<object> type)
    {
        Registry<EntityType<object>>.Register(BuiltInRegistries.ENTITY_TYPE, type.Id, type);
        ByRawId[type.RawId] = type;
    }
}
