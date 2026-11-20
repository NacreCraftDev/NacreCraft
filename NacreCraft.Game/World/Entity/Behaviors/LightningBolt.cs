using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//LightningBolt a lightning strike entity, maps to vanilla net.minecraft.world.entity.LightningBolt
//NC has no weather/lightning rendering or thunder sounds, so the strike only damages living entities in the vanilla radius and
//flashes for the vanilla lifetime; fire ignition, lightning rods and copper oxidation are not applied
public sealed class LightningBolt : NacreCraft.Registry.Entity
{
    private const double DamageRadius = 3.0;
    private int _life = 2;
    private int _flashes;
    private bool _visualOnly;
    private Player? _cause;
    private readonly HashSet<NacreCraft.Registry.Entity> _hitEntities = new();
    private readonly RandomSource _random = RandomSource.Create();

    public LightningBolt()
    {
        Seed = _random.NextLong();
        _flashes = _random.NextInt(3) + 1;
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("lightning_bolt");
    public override EntityType<object>? Type => EntityTypes.LIGHTNING_BOLT;
    public override bool CanBeHitByProjectile => false;
    public override bool IsPickable => false;

    public long Seed { get; private set; }
    public void SetVisualOnly(bool visualOnly) => _visualOnly = visualOnly;
    public Player? GetCause() => _cause;
    public void SetCause(Player? cause) => _cause = cause;
    public int GetBlocksSetOnFire() => 0;

    public override void Tick()
    {
        TickBase();
        if (Level is not ServerLevel level)
        {
            if (_life >= 0) _life--;
            if (_life < 0) Discard();
            return;
        }
        if (_life == 2 && !_visualOnly)
        {
            var box = new AABB(Pos.X - DamageRadius, Pos.Y - DamageRadius, Pos.Z - DamageRadius,
                Pos.X + DamageRadius, Pos.Y + 6 + DamageRadius, Pos.Z + DamageRadius);
            foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box))
            {
                if (living.IsDeadOrDying || _hitEntities.Contains(living)) continue;
                living.HurtServer(level, living.DamageSources().LightningBolt(), 5f);
                _hitEntities.Add(living);
            }
        }
        _life--;
        if (_life < 0)
        {
            if (_flashes == 0)
            {
                Discard();
                return;
            }
            if (_life < -_random.NextInt(10))
            {
                _flashes--;
                _life = 1;
                Seed = _random.NextLong();
            }
        }
    }
}
