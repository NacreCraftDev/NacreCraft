using NacreCraft.Nbt;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//TamableAnimal an ageable mob that can be tamed and ordered to sit, maps to vanilla net.minecraft.world.entity.TamableAnimal
//Owner, tame, ordered-to-sit and sitting-pose state are ported; the follow/sit AI goals are not, because NC has no goal selector yet
public abstract class TamableAnimal : AgeableMob
{
    private EntityReference<LivingEntity>? _owner;
    private bool _tame;
    private bool _orderedToSit;
    private bool _inSittingPose;

    protected TamableAnimal(NacreCraft.Registry.EntityType<object> type) : base(type)
    {
    }

    public bool IsTame() => _tame;

    public void SetTame(bool tame) => _tame = tame;

    public EntityReference<LivingEntity>? GetOwnerReference() => _owner;

    public LivingEntity? GetOwner() => _owner?.GetEntity((ServerLevel)Level!);

    public void SetOwner(LivingEntity? owner)
    {
        _owner = owner is null ? null : EntityReference<LivingEntity>.Of(owner);
        SetTame(owner is not null);
    }

    public void SetOwnerReference(EntityReference<LivingEntity>? owner)
    {
        _owner = owner;
        SetTame(owner is not null);
    }

    public bool IsOwnedBy(LivingEntity entity) => _owner is not null && _owner.Matches(entity);

    public bool IsOrderedToSit() => _orderedToSit;

    public void SetOrderedToSit(bool orderedToSit) => _orderedToSit = orderedToSit;

    public bool IsInSittingPose() => _inSittingPose;

    public void SetInSittingPose(bool inSittingPose) => _inSittingPose = inSittingPose;

    //IsSitting the vanilla convenience name for the sitting pose
    public bool IsSitting() => _inSittingPose;

    protected override bool CanBeABaby() => true;

    protected override void AddAdditionalSaveData(CompoundTag tag)
    {
        base.AddAdditionalSaveData(tag);
        tag.PutBoolean("Tame", _tame);
        tag.PutBoolean("Sitting", _orderedToSit);
        tag.PutBoolean("InSittingPose", _inSittingPose);
        _owner?.Store(tag, "Owner");
    }

    protected override void ReadAdditionalSaveData(CompoundTag tag)
    {
        base.ReadAdditionalSaveData(tag);
        SetTame(tag.GetBooleanOr("Tame", false));
        SetOrderedToSit(tag.GetBooleanOr("Sitting", false));
        SetInSittingPose(tag.GetBooleanOr("InSittingPose", false));
        SetOwnerReference(EntityReference<LivingEntity>.Read(tag, "Owner"));
    }
}
