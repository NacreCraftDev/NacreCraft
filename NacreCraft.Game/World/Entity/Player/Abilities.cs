using NacreCraft.Codec;

namespace NacreCraft.Game.World.Entity;

//Abilities player ability flags and speeds, maps to vanilla net.minecraft.world.entity.player.Abilities
//The packed form is what the player list sends and what gets saved; flying/mayfly are the two bits the server checks when a
//client asks to start or stop flying
public sealed class Abilities
{
    public const float DefaultFlyingSpeed = 0.05f;
    public const float DefaultWalkingSpeed = 0.1f;

    //Invulnerable whether the player ignores damage, maps to vanilla invulnerable
    public bool Invulnerable { get; set; }

    //Flying whether the player is currently flying, maps to vanilla flying
    public bool Flying { get; set; }

    //MayFly whether the player is allowed to toggle flight, maps to vanilla mayfly
    public bool MayFly { get; set; }

    //Instabuild whether blocks break instantly, maps to vanilla instabuild
    public bool Instabuild { get; set; }

    //MayBuild whether the player may place/break blocks, maps to vanilla mayBuild, defaults to true
    public bool MayBuild { get; set; } = true;

    //FlyingSpeed flight speed multiplier, maps to vanilla flyingSpeed
    public float FlyingSpeed { get; set; } = DefaultFlyingSpeed;

    //WalkingSpeed walking speed multiplier, maps to vanilla walkingSpeed
    public float WalkingSpeed { get; set; } = DefaultWalkingSpeed;

    //Pack snapshots the abilities, maps to vanilla pack
    public Packed Pack()
        => new(Invulnerable, Flying, MayFly, Instabuild, MayBuild, FlyingSpeed, WalkingSpeed);

    //Apply copies a packed snapshot back, maps to vanilla apply
    public void Apply(Packed packed)
    {
        Invulnerable = packed.Invulnerable;
        Flying = packed.Flying;
        MayFly = packed.MayFly;
        Instabuild = packed.Instabuild;
        MayBuild = packed.MayBuild;
        FlyingSpeed = packed.FlyingSpeed;
        WalkingSpeed = packed.WalkingSpeed;
    }

    //Packed the network/save form of the abilities, maps to vanilla Abilities.Packed
    public sealed record Packed(
        bool Invulnerable,
        bool Flying,
        bool MayFly,
        bool Instabuild,
        bool MayBuild,
        float FlyingSpeed,
        float WalkingSpeed)
    {
        public static readonly Codec<Packed> CODEC = RecordCodecBuilder.Of7(
            Codecs.Bool.OptionalFieldOf("invulnerable", false).ForGetter((Packed p) => p.Invulnerable),
            Codecs.Bool.OptionalFieldOf("flying", false).ForGetter((Packed p) => p.Flying),
            Codecs.Bool.OptionalFieldOf("mayfly", false).ForGetter((Packed p) => p.MayFly),
            Codecs.Bool.OptionalFieldOf("instabuild", false).ForGetter((Packed p) => p.Instabuild),
            Codecs.Bool.OptionalFieldOf("mayBuild", true).ForGetter((Packed p) => p.MayBuild),
            Codecs.Float.OptionalFieldOf("flySpeed", DefaultFlyingSpeed).ForGetter((Packed p) => p.FlyingSpeed),
            Codecs.Float.OptionalFieldOf("walkSpeed", DefaultWalkingSpeed).ForGetter((Packed p) => p.WalkingSpeed),
            (invulnerable, flying, mayFly, instabuild, mayBuild, flyingSpeed, walkingSpeed)
                => new Packed(invulnerable, flying, mayFly, instabuild, mayBuild, flyingSpeed, walkingSpeed));
    }
}
