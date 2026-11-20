using NacreCraft.Primitives;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//ProjectileDeflection how a projectile reacts to a deflector, maps to vanilla net.minecraft.world.entity.projectile.ProjectileDeflection
//The deflector may reverse the projectile, aim it along the defender's look or keep its momentum; NC has no per-entity deflection
//callback yet, so projectiles hold the deflection themselves
public interface ProjectileDeflection
{
    //None no deflection, the projectile keeps flying, maps to vanilla NONE
    public static readonly ProjectileDeflection None = new DelegateDeflection((_, _, _) => { });

    //Reverse halves the velocity and spins the projectile around, maps to vanilla REVERSE
    public static readonly ProjectileDeflection Reverse = new DelegateDeflection((projectile, _, random) =>
    {
        var rotation = 170.0f + random.NextFloat() * 20.0f;
        projectile.Velocity = projectile.Velocity.Multiply(-0.5);
        projectile.YRot += rotation;
    });

    //AimDeflect sends the projectile along the deflector's look angle, maps to vanilla AIM_DEFLECT
    public static readonly ProjectileDeflection AimDeflect = new DelegateDeflection((projectile, entity, _) =>
    {
        if (entity is not null) projectile.Velocity = LookAngle(entity);
    });

    //MomentumDeflect sends the projectile along the deflector's velocity, maps to vanilla MOMENTUM_DEFLECT
    public static readonly ProjectileDeflection MomentumDeflect = new DelegateDeflection((projectile, entity, _) =>
    {
        if (entity is not null) projectile.Velocity = entity.Velocity.Normalize();
    });

    void Deflect(Projectile projectile, NacreCraft.Registry.Entity? entity, RandomSource random);

    //LookAngle builds a unit look vector from the entity facing, maps to vanilla Entity.getLookAngle
    internal static Vec3 LookAngle(NacreCraft.Registry.Entity entity)
    {
        var yRot = entity.YRot * (Math.PI / 180.0);
        var xRot = entity.XRot * (Math.PI / 180.0);
        return new Vec3(-Math.Sin(yRot) * Math.Cos(xRot), -Math.Sin(xRot), Math.Cos(yRot) * Math.Cos(xRot));
    }
}

//DelegateDeflection adapter around a lambda, maps to the vanilla functional interface
internal sealed class DelegateDeflection(
    Action<Projectile, NacreCraft.Registry.Entity?, RandomSource> deflect) : ProjectileDeflection
{
    public void Deflect(Projectile projectile, NacreCraft.Registry.Entity? entity, RandomSource random)
        => deflect(projectile, entity, random);
}
