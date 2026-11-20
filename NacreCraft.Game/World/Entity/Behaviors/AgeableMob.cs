using NacreCraft.Nbt;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//AgeableMob a mob with an age and a baby form, maps to vanilla net.minecraft.world.entity.AgeableMob
//The age is negative while the mob is a baby and counts up to zero; forced age is applied after the natural age reaches zero.
//NC has no synced baby flag yet, so IsBaby is derived from the age directly
public abstract class AgeableMob : PathfinderMob
{
    public const int BabyStartAge = -24000;
    public const int AgeLockCooldownTicks = 40;
    protected const int DefaultAge = 0;
    protected const int DefaultForcedAge = 0;

    protected int _age = DefaultAge;
    protected int _forcedAge = DefaultForcedAge;
    protected int _forcedAgeTimer;
    protected int _ageLockParticleTimer;

    protected AgeableMob(NacreCraft.Registry.EntityType<object> type) : base(type)
    {
    }

    public abstract AgeableMob? GetBreedOffspring(ServerLevel level, AgeableMob partner);

    public virtual bool CanBreed() => false;

    public virtual bool IsBaby => GetAge() < 0;

    public int GetAge() => _age;

    public int GetBabyStartAge() => BabyStartAge;

    public void SetAge(int newAge)
    {
        var oldAge = GetAge();
        _age = newAge;
        if ((oldAge < 0 && newAge >= 0) || (oldAge >= 0 && newAge < 0)) AgeBoundaryReached();
    }

    //AgeBoundaryReached called when the mob switches between baby and adult
    protected virtual void AgeBoundaryReached()
    {
    }

    //AgeUp advances the age by the given seconds, maps to vanilla ageUp
    //Forced growth accumulates while the natural age is still negative and is applied once it reaches zero
    public void AgeUp(int seconds, bool forced)
    {
        var age = GetAge();
        var newAge = age + seconds * 20;
        if (newAge > 0) newAge = 0;
        var delta = newAge - age;
        SetAge(newAge);
        if (forced)
        {
            _forcedAge += delta;
            if (_forcedAgeTimer == 0) _forcedAgeTimer = 40;
        }
        if (GetAge() == 0) SetAge(_forcedAge);
    }

    public void AgeUp(int seconds) => AgeUp(seconds, false);

    //TickAge advances the forced-age timer; the natural age increases elsewhere through Tick
    protected void TickAge()
    {
        if (_forcedAgeTimer > 0)
        {
            if (_forcedAgeTimer % 4 == 0) _forcedAgeTimer--;
            else _forcedAgeTimer--;
        }
    }

    protected virtual bool CanBeABaby() => true;

    protected override void AddAdditionalSaveData(CompoundTag tag)
    {
        base.AddAdditionalSaveData(tag);
        if (!CanBeABaby()) return;
        tag.PutInt("Age", GetAge());
        tag.PutInt("ForcedAge", _forcedAge);
    }

    protected override void ReadAdditionalSaveData(CompoundTag tag)
    {
        base.ReadAdditionalSaveData(tag);
        if (!CanBeABaby()) return;
        SetAge(tag.GetIntValue("Age"));
        _forcedAge = tag.GetIntValue("ForcedAge");
    }
}
