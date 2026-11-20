using NacreCraft.Registry;
using NacreCraft.Util;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity;

//ItemBasedSteering a boost state driven by a synced boost-time field, maps to vanilla net.minecraft.world.entity.ItemBasedSteering
//Used by pigs and striders: the boost sets a random duration and the factor follows a sine curve until it expires
public sealed class ItemBasedSteering
{
    private const int MinBoostTime = 140;
    private const int MaxBoostTime = 700;

    private readonly SynchedEntityData _entityData;
    private readonly EntityDataAccessor<int> _boostTimeAccessor;
    private bool _boosting;
    private int _boostTime;

    public ItemBasedSteering(SynchedEntityData entityData, EntityDataAccessor<int> boostTimeAccessor)
    {
        _entityData = entityData;
        _boostTimeAccessor = boostTimeAccessor;
    }

    //OnSynced starts boosting when the synced value arrives, maps to vanilla onSynced
    public void OnSynced()
    {
        _boosting = true;
        _boostTime = 0;
    }

    //Boost starts a boost with a random duration, false when already boosting
    public bool Boost(RandomSource random)
    {
        if (_boosting) return false;
        _boosting = true;
        _boostTime = 0;
        _entityData.Set(_boostTimeAccessor, random.NextInt(MaxBoostTime - MinBoostTime + 1) + MinBoostTime);
        return true;
    }

    //TickBoost advances the boost timer, maps to vanilla tickBoost
    public void TickBoost()
    {
        if (!_boosting) return;
        var time = _boostTime;
        _boostTime = time + 1;
        if (time > BoostTimeTotal()) _boosting = false;
    }

    //BoostFactor the speed multiplier of the current boost, maps to vanilla boostFactor
    public float BoostFactor()
        => _boosting ? 1f + 1.15f * Mth.Sin(_boostTime / (float)BoostTimeTotal() * MathF.PI) : 1f;

    private int BoostTimeTotal() => _entityData.Get(_boostTimeAccessor);
}
