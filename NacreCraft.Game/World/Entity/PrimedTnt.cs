using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//PrimedTnt primed TNT, maps to vanilla net.minecraft.world.entity.item.PrimedTnt
//Tossed slightly on spawn, falls under gravity and blasts when the fuse runs out
//The fuse only lives on the server; vanilla also syncs it for the client flashing, which is not wired up here
public sealed class PrimedTnt : NacreCraft.Registry.Entity
{
    //DefaultFuseTime ticks from ignition to blast, maps to vanilla DEFAULT_FUSE_TIME
    public const int DefaultFuseTime = 80;

    //NoFuse sentinel for one that never goes off, maps to vanilla NO_FUSE
    public const int NoFuse = -1;

    //DefaultExplosionPower blast radius, maps to vanilla DEFAULT_EXPLOSION_POWER
    private const float DefaultExplosionPower = 4.0f;

    //BounceFactor vertical speed factor on landing, maps to vanilla -0.5
    private const double BounceFactor = -0.5;

    //GroundHorizontalFactor horizontal speed factor on landing, maps to vanilla 0.7
    private const double GroundHorizontalFactor = 0.7;

    private readonly EntityType<object> _type;

    public PrimedTnt(EntityType<object> type) => _type = type;

    //PrimedTnt spawned by ignition at a block centre, maps to the vanilla position constructor
    public PrimedTnt(EntityType<object> type, double x, double y, double z, NacreCraft.Registry.Entity? owner)
        : this(type)
    {
        Pos = new Vec3(x, y, z);
        //The initial toss points in a random direction, which is what makes a row of TNT spread out instead of stacking
        var rotation = Random.Shared.NextDouble() * Math.PI * 2.0;
        Velocity = new Vec3(-Math.Sin(rotation) * 0.02, 0.2, -Math.Cos(rotation) * 0.02);
        Fuse = DefaultFuseTime;
        Owner = owner;
    }

    public override Identifier Id => _type.Id;

    public override EntityType<object>? Type => _type;

    //BlocksBuilding TNT blocks placement like a solid block, maps to vanilla blocksBuilding
    public override bool BlocksBuilding => true;

    //DefaultGravity TNT gravity 0.04, less than the default 0.08, maps to vanilla getDefaultGravity
    public override double DefaultGravity => 0.04;

    //Fuse ticks left before the blast, maps to vanilla fuse
    public int Fuse { get; set; } = DefaultFuseTime;

    //ExplosionPower blast radius, maps to vanilla explosionPower
    public float ExplosionPower { get; set; } = DefaultExplosionPower;

    //Owner the entity credited for the ignition, maps to vanilla getOwner
    public NacreCraft.Registry.Entity? Owner { get; set; }

    //Tick falls and counts the fuse down, then goes off, maps to vanilla PrimedTnt.tick
    //The portal and fluid interaction branches are skipped: this project has neither
    public override void Tick()
    {
        TickBase();
        Velocity = new Vec3(Velocity.X, Velocity.Y - DefaultGravity, Velocity.Z);
        Move(Velocity);
        var airDrag = AirDrag;
        Velocity = new Vec3(Velocity.X * airDrag, Velocity.Y * airDrag, Velocity.Z * airDrag);
        if (OnGround)
            Velocity = new Vec3(Velocity.X * GroundHorizontalFactor, Velocity.Y * BounceFactor,
                Velocity.Z * GroundHorizontalFactor);

        Fuse--;
        if (Fuse > 0) return;
        Discard();
        Explode();
    }

    //Explode runs the blast, maps to vanilla PrimedTnt.explode
    //The blast is skipped outright when tnt_explodes is off, which is what lets a server disable TNT without a restart
    private void Explode()
    {
        if (Level is not NacreCraft.Storage.ServerLevel level) return;
        if (level.GameRules?.GetBool(NacreCraft.Game.World.Level.GameRules.TntExplodes) == false) return;
        var decay = level.GameRules?.GetBool(NacreCraft.Game.World.Level.GameRules.TntExplosionDropDecay) ?? false;
        var interaction = decay
            ? NacreCraft.Game.World.Level.BlockInteraction.DestroyWithDecay
            : NacreCraft.Game.World.Level.BlockInteraction.Destroy;
        //Vanilla measures the blast from slightly above the centre, maps to getY(0.0625)
        var center = new Vec3(Pos.X, Pos.Y + 0.0625, Pos.Z);
        new NacreCraft.Game.World.Level.ServerExplosion(level, this, null, null, center, ExplosionPower, false, interaction)
            .Explode();
    }

    //GetRandomShortFuse shortens a fuse when TNT is set off by another blast, maps to vanilla getRandomShortFuse
    //Without it a chain reaction of TNT all goes off on the same tick
    public static int GetRandomShortFuse(int fuse, RandomSource random)
        => random.NextInt(Math.Max(1, fuse / 4)) + fuse / 8;
}
