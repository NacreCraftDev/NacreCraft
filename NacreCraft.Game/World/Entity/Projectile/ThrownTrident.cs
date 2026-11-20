using NacreCraft.Game.World.Items;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//ThrownTrident a thrown trident, maps to vanilla net.minecraft.world.entity.projectile.arrow.ThrownTrident
//Deals eight points on hit and returns to a owner with loyalty; NC has no enchantment query yet, so loyalty is a settable byte
//and the item does not carry enchantments into the entity
public sealed class ThrownTrident : AbstractArrow
{
    private const float TridentDamage = 8f;
    private const float WaterInertia = 0.99f;
    private const int LoyaltyReturnSpeed = 1;

    private bool _dealtDamage;
    private byte _loyalty;
    private bool _foil;
    private LivingEntity? _owner;

    public ThrownTrident(EntityType<object> type) : base(type)
    {
    }

    public ThrownTrident(EntityType<object> type, LivingEntity owner, ItemStack tridentItem) : this(type)
    {
        _owner = owner;
        SetOwner(owner);
    }

    public byte Loyalty => _loyalty;

    public void SetLoyalty(byte loyalty) => _loyalty = loyalty;

    public bool IsFoil() => _foil;

    public void SetFoil(bool foil) => _foil = foil;

    public override double AirDrag => WaterInertia;

    public override void Tick()
    {
        if (InGroundTime > 4) _dealtDamage = true;
        var owner = ResolveOwner();
        if (_loyalty > 0 && _dealtDamage && owner is not null)
        {
            if (owner.IsDeadOrDying || owner is Player player && player.IsSpectator())
            {
                Discard();
                return;
            }
            var toOwner = owner.GetEyePosition().Subtract(Pos);
            SetPos(Pos.X, Pos.Y + toOwner.Y * 0.015 * _loyalty, Pos.Z);
            var acceleration = 0.05 * _loyalty;
            Velocity = Velocity.Multiply(0.95).Add(toOwner.Normalize().Multiply(acceleration));
        }
        base.Tick();
    }

    protected override void OnHitEntity(ProjectileHitResult hit)
    {
        if (hit.Entity is not LivingEntity target || Level is not ServerLevel level) return;
        _dealtDamage = true;
        var owner = ResolveOwner();
        target.HurtServer(level, target.DamageSources().Trident(this, owner), TridentDamage);
        Velocity = new Vec3(Velocity.X * 0.02, Velocity.Y * 0.2, Velocity.Z * 0.02);
    }

    private LivingEntity? ResolveOwner()
    {
        if (_owner is { IsRemoved: false }) return _owner;
        if (OwnerUuid is { } uuid && Level is ServerLevel level && level.GetEntity(uuid) is LivingEntity owner)
        {
            _owner = owner;
            return owner;
        }
        return null;
    }
}
