using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;
using Mth = NacreCraft.Util.Mth;

namespace NacreCraft.Game.World.Entity;

//LlamaSpit a llama spit projectile, maps to vanilla net.minecraft.world.entity.projectile.LlamaSpit
//Deals one point of spit damage and discards on block/water contact; NC has no llama entity yet, so the owner is any living entity
public sealed class LlamaSpit : Projectile
{
    private readonly RandomSource _random = RandomSource.Create();
    private LivingEntity? _owner;

    public LlamaSpit(EntityType<object> type) : base(type)
    {
    }

    public LlamaSpit(EntityType<object> type, LivingEntity owner) : this(type)
    {
        _owner = owner;
        SetOwner(owner);
        var bodyRot = owner.YRot * Mth.DegToRad;
        var offset = (owner.Width + 1.0) * 0.5;
        SetPos(owner.Pos.X - offset * Mth.Sin(bodyRot), owner.Pos.Y + owner.EyeHeight - 0.1, owner.Pos.Z + offset * Mth.Cos(bodyRot));
    }

    public override double DefaultGravity => 0.06;

    public override double AirDrag => 0.99;

    public override void Tick()
    {
        TickBase();
        var movement = Velocity;
        var hit = ProjectileUtil.GetHitResult(this);
        if (hit.Type != ProjectileHitType.miss) HitTargetOrDeflectSelf(hit, hit.Entity, _random);
        UpdateRotation();
        if (Level is PersistentServerLevel level && level.GetFluidState(BlockPosition()).IsEmpty == false)
        {
            Discard();
            return;
        }
        Velocity = movement.Multiply(AirDrag).Add(0, -DefaultGravity, 0);
        SetPos(Pos.X + movement.X, Pos.Y + movement.Y, Pos.Z + movement.Z);
    }

    protected override void OnHitEntity(ProjectileHitResult hit)
    {
        if (hit.Entity is LivingEntity target && _owner is not null && Level is PersistentServerLevel level)
        {
            if (target.HurtServer(level, target.DamageSources().Spit(this, _owner), 1.0f))
            {
                //Post-attack enchantment effects are applied once the enchantment helper is ported
            }
        }
        if (Level is PersistentServerLevel) Discard();
    }

    protected override void OnHitBlock(ProjectileHitResult hit)
    {
        if (Level is PersistentServerLevel) Discard();
    }
}
