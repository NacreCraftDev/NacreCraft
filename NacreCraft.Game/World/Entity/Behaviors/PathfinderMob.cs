using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//PathfinderMob a mob that walks with a path navigator, maps to vanilla net.minecraft.world.entity.PathfinderMob
//NC has no goal selector, brain or navigation yet, so panicking and path finding report false and the walk target value is flat;
//the leash-follow helpers are the parts that do not need those systems
public abstract class PathfinderMob : Mob
{
    protected const float DefaultWalkTargetValue = 0f;

    protected PathfinderMob(NacreCraft.Registry.EntityType<object> type) : base(type)
    {
    }

    //GetWalkTargetValue the desirability of standing on a block, maps to vanilla getWalkTargetValue
    public float GetWalkTargetValue(BlockPos pos) => DefaultWalkTargetValue;

    public float GetWalkTargetValue(BlockPos pos, ServerLevel level) => DefaultWalkTargetValue;

    //CheckSpawnRules whether the spawn position is acceptable, maps to vanilla checkSpawnRules
    public virtual bool CheckSpawnRules(object level, EntitySpawnReason spawnReason)
        => GetWalkTargetValue(BlockPosition()) >= 0f;

    //IsPathFinding whether a path is being followed; no navigation exists, so always false
    public bool IsPathFinding() => false;

    //IsPanicking whether a panic goal is running; no goal selector exists, so always false
    public bool IsPanicking() => false;

    //ShouldStayCloseToLeashHolder whether leash following pulls the mob toward its holder
    protected virtual bool ShouldStayCloseToLeashHolder() => true;

    //FollowLeashSpeed the speed used while following a leash holder
    protected virtual double FollowLeashSpeed() => 1.0;
}
