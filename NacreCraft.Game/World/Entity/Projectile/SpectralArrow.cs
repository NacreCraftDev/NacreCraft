using NacreCraft.Game.World.Effect;
using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Entity;

//SpectralArrow a spectral arrow, maps to vanilla net.minecraft.world.entity.projectile.arrow.SpectralArrow
//Deals the normal arrow damage and applies glowing to the hit living entity; NC has no arrow pickup stack yet
public sealed class SpectralArrow : AbstractArrow
{
    private const int DefaultDuration = 200;

    private int _duration = DefaultDuration;

    public SpectralArrow(EntityType<object> type) : base(type)
    {
    }

    public int Duration => _duration;

    public void SetDuration(int duration) => _duration = duration;

    protected override void OnHitEntity(ProjectileHitResult hit)
    {
        base.OnHitEntity(hit);
        if (hit.Entity is LivingEntity living && !living.IsDeadOrDying)
            living.AddEffect(new MobEffectInstance(MobEffects.GLOWING, _duration, 0, false, true));
    }

    protected override void AddAdditionalSaveData(CompoundTag tag)
    {
        base.AddAdditionalSaveData(tag);
        tag.PutInt("Duration", _duration);
    }

    protected override void ReadAdditionalSaveData(CompoundTag tag)
    {
        base.ReadAdditionalSaveData(tag);
        _duration = tag.GetIntOr("Duration", DefaultDuration);
    }
}
