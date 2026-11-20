using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//EvokerFangs the evoker's fang attack, maps to vanilla net.minecraft.world.entity.projectile.EvokerFangs
//Warms up, damages every living entity in the fang box once, then disappears; NC has no client animation event, so the
//animation progress is still queryable by the renderer
public sealed class EvokerFangs : NacreCraft.Registry.Entity
{
    public const int AttackDuration = 20;
    public const int LifeOffset = 2;
    public const int AttackTriggerTicks = 14;

    private int _warmupDelayTicks;
    private bool _sentSpikeEvent;
    private int _lifeTicks = 22;
    private bool _clientSideAttackStarted;
    private EntityReference<LivingEntity>? _owner;

    public EvokerFangs(EntityType<object> type) : base()
    {
    }

    public EvokerFangs(EntityType<object> type, double x, double y, double z, float rotationRadians,
        int warmupDelayTicks, LivingEntity owner) : this(type)
    {
        _warmupDelayTicks = warmupDelayTicks;
        SetOwner(owner);
        YRot = rotationRadians * 57.295776f;
        SetPos(x, y, z);
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("evoker_fangs");

    public override EntityType<object>? Type => EntityTypes.EVOKER_FANGS;

    public int WarmupDelayTicks => _warmupDelayTicks;

    public void SetOwner(LivingEntity owner) => _owner = EntityReference<LivingEntity>.Of(owner);

    public LivingEntity? GetOwner() => Level is ServerLevel level ? _owner?.GetEntity(level) : _owner?.GetEntity();

    public override void Tick()
    {
        TickBase();
        if (Level is not ServerLevel level)
        {
            if (_clientSideAttackStarted && _lifeTicks > 0) _lifeTicks--;
            return;
        }
        _warmupDelayTicks--;
        if (_warmupDelayTicks >= 0) return;
        if (_warmupDelayTicks == -8)
        {
            var box = BoundingBox.Inflate(0.2, 0.0, 0.2);
            foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box)) DealDamageTo(living);
        }
        if (!_sentSpikeEvent)
        {
            _clientSideAttackStarted = true;
            _sentSpikeEvent = true;
        }
        _lifeTicks--;
        if (_lifeTicks < 0) Discard();
    }

    private void DealDamageTo(LivingEntity entity)
    {
        var owner = GetOwner();
        if (entity.IsDeadOrDying || ReferenceEquals(entity, owner)) return;
        if (Level is not ServerLevel level) return;
        var source = owner is null ? entity.DamageSources().Magic() : entity.DamageSources().IndirectMagic(this, owner);
        entity.HurtServer(level, source, 6.0f);
    }

    //GetAnimationProgress fang opening progress in [0,1], maps to vanilla getAnimationProgress
    public float GetAnimationProgress(float partialTick)
    {
        if (!_clientSideAttackStarted) return 0.0f;
        var remainingLife = _lifeTicks - LifeOffset;
        if (remainingLife <= 0) return 1.0f;
        return 1.0f - (remainingLife - partialTick) / 20.0f;
    }

    public override bool CanBeHitByProjectile => false;

    public override bool IsPickable => false;
}
