using NacreCraft.Game.World.Effect;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//ShulkerBullet a homing shulker projectile, maps to vanilla net.minecraft.world.entity.projectile.ShulkerBullet
//NC has no block-grid path selection, so the bullet homes straight at the target and only re-aims on a timer or when blocked;
//hit effects (levitation and knockback) are ported
public sealed class ShulkerBullet : Projectile
{
    private const double Speed = 0.15;

    private readonly RandomSource _random = RandomSource.Create();
    private LivingEntity? _owner;
    private NacreCraft.Registry.Entity? _target;
    private Direction? _moveDirection;
    private int _flightSteps;
    private double _targetDeltaX;
    private double _targetDeltaY;
    private double _targetDeltaZ;

    public ShulkerBullet(EntityType<object> type) : base(type)
    {
    }

    public ShulkerBullet(EntityType<object> type, LivingEntity owner, NacreCraft.Registry.Entity? target,
        Direction.Axis avoidAxis) : this(type)
    {
        _owner = owner;
        SetOwner(owner);
        var center = new Vec3(owner.Pos.X, owner.Pos.Y + owner.Height * 0.5, owner.Pos.Z);
        SetPos(center);
        _target = target;
        _moveDirection = Direction.Up;
        SelectNextMoveDirection(avoidAxis, target);
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("shulker_bullet");

    public override EntityType<object>? Type => EntityTypes.SHULKER_BULLET;

    public NacreCraft.Registry.Entity? GetTarget() => _target;

    public Direction? GetMoveDirection() => _moveDirection;

    public override double DefaultGravity => 0.04;

    public override bool IsPickable => true;

    public override void Tick()
    {
        TickBase();
        if (Level is not ServerLevel level) return;
        if (_target is not null && _target.IsRemoved)
        {
            _target = null;
            _moveDirection = null;
        }
        if (_target is not null)
        {
            _targetDeltaX = Math.Clamp(_targetDeltaX * 1.025, -1.0, 1.0);
            _targetDeltaY = Math.Clamp(_targetDeltaY * 1.025, -1.0, 1.0);
            _targetDeltaZ = Math.Clamp(_targetDeltaZ * 1.025, -1.0, 1.0);
            var movement = Velocity;
            Velocity = movement.Add((_targetDeltaX - movement.X) * 0.2, (_targetDeltaY - movement.Y) * 0.2,
                (_targetDeltaZ - movement.Z) * 0.2);
        }
        else
        {
            Velocity = Velocity.Add(0.0, -DefaultGravity, 0.0);
        }
        var step = Velocity;
        SetPos(Pos.Add(step));
        var hit = ProjectileUtil.GetHitResult(this);
        if (hit.Type != ProjectileHitType.miss && !IsRemoved) HitTargetOrDeflectSelf(hit, hit.Entity, _random);
        UpdateRotation();
        if (_target is not null && _flightSteps > 0)
        {
            _flightSteps--;
            if (_flightSteps == 0) SelectNextMoveDirection(_moveDirection?.GetAxis(), _target);
        }
    }

    //SelectNextMoveDirection aims at the target directly, maps to vanilla selectNextMoveDirection without the block-grid search
    private void SelectNextMoveDirection(Direction.Axis? avoidAxis, NacreCraft.Registry.Entity? target)
    {
        var targetX = target?.Pos.X ?? Pos.X;
        var targetY = target is null ? Pos.Y - 1 : target.Pos.Y + target.Height * 0.5;
        var targetZ = target?.Pos.Z ?? Pos.Z;
        var xa = targetX - Pos.X;
        var ya = targetY - Pos.Y;
        var za = targetZ - Pos.Z;
        var distance = Math.Sqrt(xa * xa + ya * ya + za * za);
        if (distance == 0.0)
        {
            _targetDeltaX = 0.0;
            _targetDeltaY = 0.0;
            _targetDeltaZ = 0.0;
        }
        else
        {
            _targetDeltaX = xa / distance * Speed;
            _targetDeltaY = ya / distance * Speed;
            _targetDeltaZ = za / distance * Speed;
        }
        _moveDirection = DominantDirection(_targetDeltaX, _targetDeltaY, _targetDeltaZ);
        _flightSteps = 10 + _random.NextInt(5) * 10;
    }

    private static Direction DominantDirection(double x, double y, double z)
    {
        var ax = Math.Abs(x);
        var ay = Math.Abs(y);
        var az = Math.Abs(z);
        if (ay >= ax && ay >= az) return y >= 0 ? Direction.Up : Direction.Down;
        if (ax >= az) return x >= 0 ? Direction.East : Direction.West;
        return z >= 0 ? Direction.South : Direction.North;
    }

    protected override void OnHitEntity(ProjectileHitResult hit)
    {
        if (hit.Entity is not LivingEntity target || Level is not ServerLevel level) return;
        if (target.HurtServer(level, target.DamageSources().MobProjectile(this, _owner), 4.0f))
        {
            target.AddEffect(new MobEffectInstance(MobEffects.LEVITATION, 200, 0, false, true));
        }
        Discard();
    }

    protected override void OnHitBlock(ProjectileHitResult hit) => Discard();
}
