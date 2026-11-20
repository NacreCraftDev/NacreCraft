using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//FireworkRocketEntity a flying firework rocket, maps to vanilla net.minecraft.world.entity.projectile.FireworkRocketEntity
//It boosts an elytra flyer while attached, otherwise accelerates upward and explodes after its flight time; NC rappels the
//explosion through a flat radius because the line-of-sight clip is not exposed by the storage layer
public sealed class FireworkRocketEntity : Projectile, ItemSupplier
{
    private readonly RandomSource _random = RandomSource.Create();
    private ItemStack _item;
    private int _life;
    private int _lifetime;
    private bool _shotAtAngle;
    private LivingEntity? _attachedTo;
    private NacreCraft.Registry.Entity? _owner;

    public FireworkRocketEntity(EntityType<object> type) : base(type)
    {
        _item = ItemSupplier.DefaultStack("firework_rocket");
    }

    public FireworkRocketEntity(EntityType<object> type, double x, double y, double z, ItemStack source) : this(type)
    {
        SetPos(x, y, z);
        _item = source.Copy();
        var flights = 1 + GetFireworks().FlightDuration;
        Velocity = new Vec3(Triangle(0.0, 0.002297), 0.05, Triangle(0.0, 0.002297));
        _lifetime = 10 * flights + _random.NextInt(6) + _random.NextInt(7);
    }

    public FireworkRocketEntity(EntityType<object> type, NacreCraft.Registry.Entity owner, double x, double y,
        double z, ItemStack source) : this(type, x, y, z, source)
    {
        _owner = owner;
        SetOwner(owner);
    }

    public FireworkRocketEntity(EntityType<object> type, ItemStack source, LivingEntity attachedTo) : this(type,
        attachedTo, attachedTo.Pos.X, attachedTo.Pos.Y, attachedTo.Pos.Z, source)
    {
        _attachedTo = attachedTo;
    }

    public FireworkRocketEntity(EntityType<object> type, ItemStack source, double x, double y, double z,
        bool shotAtAngle) : this(type, x, y, z, source)
    {
        _shotAtAngle = shotAtAngle;
    }

    public FireworkRocketEntity(EntityType<object> type, ItemStack source, NacreCraft.Registry.Entity owner,
        double x, double y, double z, bool shotAtAngle) : this(type, source, x, y, z, shotAtAngle)
    {
        _owner = owner;
        SetOwner(owner);
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("firework_rocket");

    public override EntityType<object>? Type => EntityTypes.FIREWORK_ROCKET;

    public ItemStack GetItem() => _item;

    public bool IsShotAtAngle() => _shotAtAngle;

    public bool IsAttachedToEntity() => _attachedTo is not null;

    public override bool IsPickable => false;

    public override bool CanBeHitByProjectile => false;

    public override void Tick()
    {
        TickBase();
        if (_attachedTo is not null)
        {
            if (_attachedTo.IsFallFlying)
            {
                var look = ProjectileDeflection.LookAngle(_attachedTo);
                var movement = _attachedTo.Velocity;
                _attachedTo.Velocity = movement.Add(
                    look.X * 0.1 + (look.X * 1.5 - movement.X) * 0.5,
                    look.Y * 0.1 + (look.Y * 1.5 - movement.Y) * 0.5,
                    look.Z * 0.1 + (look.Z * 1.5 - movement.Z) * 0.5);
            }
            SetPos(_attachedTo.Pos);
            Velocity = _attachedTo.Velocity;
        }
        else
        {
            if (!_shotAtAngle)
            {
                var horizontal = HorizontalCollision ? 1.0 : 1.15;
                Velocity = new Vec3(Velocity.X * horizontal, Velocity.Y + 0.04, Velocity.Z * horizontal);
            }
            var movement = Velocity;
            var hit = ProjectileUtil.GetHitResult(this);
            Move(movement);
            Velocity = movement;
            if (hit.Type != ProjectileHitType.miss && !IsRemoved)
            {
                HitTargetOrDeflectSelf(hit, hit.Entity, _random);
                return;
            }
        }
        UpdateRotation();
        _life++;
        if (_life > _lifetime) Explode();
    }

    private void Explode()
    {
        if (Level is not ServerLevel level)
        {
            Discard();
            return;
        }
        var explosions = GetExplosions();
        var damageAmount = explosions.Count == 0 ? 0f : 5.0f + explosions.Count * 2;
        if (damageAmount > 0f)
        {
            if (_attachedTo is not null)
                _attachedTo.HurtServer(level, _attachedTo.DamageSources().Fireworks(this, _owner), 5.0f + explosions.Count * 2);
            var box = BoundingBox.Inflate(5.0, 5.0, 5.0);
            foreach (var target in level.GetEntitiesOfClass<LivingEntity>(box))
            {
                if (ReferenceEquals(target, _attachedTo) || target.IsDeadOrDying) continue;
                var distance = Math.Sqrt(target.Pos.DistanceToSqr(Pos));
                if (distance > 5.0) continue;
                var damage = damageAmount * (float)Math.Sqrt((5.0 - distance) / 5.0);
                target.HurtServer(level, target.DamageSources().Fireworks(this, _owner), damage);
            }
        }
        Discard();
    }

    private Fireworks GetFireworks()
        => _item.Get<object>(DataComponents.FIREWORKS) as Fireworks ?? Fireworks.Default;

    private IReadOnlyList<FireworkExplosion> GetExplosions() => GetFireworks().Explosions;

    private static double Triangle(double mean, double deviation)
        => mean + deviation * (Random.Shared.NextDouble() - Random.Shared.NextDouble());
}
