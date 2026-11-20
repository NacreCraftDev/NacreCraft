using NacreCraft.Game.World.Effect;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//AreaEffectCloud a lingering potion cloud, maps to vanilla net.minecraft.world.entity.AreaEffectCloud
//NC has no cloud particle rendering, so only the effect application, radius decay and lifetime are ported
public sealed class AreaEffectCloud : NacreCraft.Registry.Entity
{
    private readonly List<MobEffectInstance> _effects = new();
    private int _age;

    public AreaEffectCloud()
    {
        Radius = 3f;
        RadiusPerTick = -0.005f;
        Duration = 600;
        WaitTime = 20;
    }

    public override Identifier Id => Identifier.WithDefaultNamespace("area_effect_cloud");
    public override bool CanBeHitByProjectile => false;
    public override bool IsPickable => false;

    public float Radius { get; set; }
    public float RadiusPerTick { get; set; }
    public int Duration { get; set; }
    public int WaitTime { get; set; }
    public int Color { get; set; } = 0xFFFFFF;
    public int Age => _age;
    public IReadOnlyList<MobEffectInstance> Effects => _effects;
    public Player? Owner { get; set; }

    public void AddEffect(MobEffectInstance effect) => _effects.Add(effect);

    public override void Tick()
    {
        TickBase();
        _age++;
        if (_age >= Duration)
        {
            Discard();
            return;
        }
        Radius += RadiusPerTick;
        if (Radius < 0.5f)
        {
            Discard();
            return;
        }
        if (_age < WaitTime || _effects.Count == 0) return;
        if (Level is not ServerLevel level) return;
        //Vanilla applies effects continuously with a per-entity reapplication delay; NC applies once per second to everything in range
        if (_age % 20 != 0) return;
        var box = new AABB(Pos.X - Radius, Pos.Y - Radius, Pos.Z - Radius, Pos.X + Radius, Pos.Y + Radius, Pos.Z + Radius);
        foreach (var living in level.GetEntitiesOfClass<LivingEntity>(box))
        {
            if (living.IsDeadOrDying || ReferenceEquals(living, Owner)) continue;
            foreach (var effect in _effects) living.AddEffect(new MobEffectInstance(effect));
        }
    }
}
