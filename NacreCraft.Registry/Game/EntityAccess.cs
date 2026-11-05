using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;

namespace NacreCraft.Registry;

//UniquelyIdentifyable anything the entity index can key, maps to vanilla net.minecraft.world.level.entity.UniquelyIdentifyable
public interface UniquelyIdentifyable
{
    Guid Uuid { get; }

    bool IsRemoved { get; }
}

//RemovalReason why an entity left the level, maps to vanilla Entity.RemovalReason
public enum RemovalReason
{
    KILLED,
    DISCARDED,
    UNLOADED_TO_CHUNK,
    UNLOADED_WITH_PLAYER,
    CHANGED_DIMENSION
}

public static class RemovalReasons
{
    //ShouldDestroy whether leaving the level also destroys the entity, maps to vanilla shouldDestroy
    public static bool ShouldDestroy(this RemovalReason reason)
        => reason is not (RemovalReason.UNLOADED_TO_CHUNK
            or RemovalReason.UNLOADED_WITH_PLAYER
            or RemovalReason.CHANGED_DIMENSION);
}

//EntityInLevelCallback the level tells an entity when it moved or was removed, maps to vanilla EntityInLevelCallback
public interface EntityInLevelCallback
{
    void OnMove();

    void OnRemove(RemovalReason reason);
}

public static class EntityInLevelCallbacks
{
    //NULL the callback used by entities outside any section manager, maps to vanilla EntityInLevelCallback.NULL
    public static readonly EntityInLevelCallback NULL = new NullCallback();

    private sealed class NullCallback : EntityInLevelCallback
    {
        public void OnMove()
        {
        }

        public void OnRemove(RemovalReason reason)
        {
        }
    }
}

//EntityAccess the narrow entity view the section index works with, maps to vanilla net.minecraft.world.level.entity.EntityAccess
public interface EntityAccess : UniquelyIdentifyable
{
    int Id { get; }

    BlockPos BlockPosition();

    AABB GetBoundingBox();

    void SetLevelCallback(EntityInLevelCallback? callback);

    IEnumerable<EntityAccess> GetSelfAndPassengers();

    IEnumerable<EntityAccess> GetPassengersAndSelf();

    void SetRemoved(RemovalReason reason);

    bool ShouldBeSaved { get; }

    bool IsAlwaysTicking { get; }
}
