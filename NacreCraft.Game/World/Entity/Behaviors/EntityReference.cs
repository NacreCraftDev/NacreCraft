using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Entity;

//EntityReference a weak reference to an entity by uuid, maps to vanilla net.minecraft.world.entity.EntityReference
//The reference keeps the entity while it is alive and falls back to a uuid lookup through the level once it was removed
public sealed class EntityReference<T> where T : NacreCraft.Registry.Entity
{
    private T? _entity;
    private Guid _uuid;

    private EntityReference(T entity)
    {
        _entity = entity;
        _uuid = entity.Uuid;
    }

    private EntityReference(Guid uuid)
    {
        _uuid = uuid;
    }

    //Of a live entity reference, null stays null
    public static EntityReference<T>? Of(T? entity) => entity is null ? null : new EntityReference<T>(entity);

    //Of a uuid-only reference
    public static EntityReference<T> Of(Guid uuid) => new(uuid);

    public Guid GetUuid() => _entity?.Uuid ?? _uuid;

    //GetEntity returns the stored live entity without a level lookup, null once it was removed
    public T? GetEntity() => _entity is { IsRemoved: false } ? _entity : null;

    //GetEntity resolves the live entity through the level when the stored instance is gone, maps to vanilla getEntity
    public T? GetEntity(ServerLevel level)
    {
        if (_entity is { IsRemoved: false }) return _entity;
        if (level.GetEntity(_uuid) is T resolved && !resolved.IsRemoved)
        {
            _entity = resolved;
            return resolved;
        }
        return null;
    }

    public bool Matches(T entity) => GetUuid() == entity.Uuid;

    //Store writes the uuid under the key, maps to vanilla store
    public void Store(CompoundTag tag, string key) => tag.PutString(key, GetUuid().ToString("D"));

    //Read reads a uuid reference, maps to vanilla read
    public static EntityReference<T>? Read(CompoundTag tag, string key)
        => Guid.TryParse(tag.GetStringValue(key), out var uuid) ? Of(uuid) : null;

    public override bool Equals(object? obj)
        => obj is EntityReference<T> other && GetUuid() == other.GetUuid();

    public override int GetHashCode() => GetUuid().GetHashCode();

    //Get resolves an entity reference of the given type through the level
    public static TEntity? Get<TEntity>(EntityReference<TEntity>? reference, ServerLevel level)
        where TEntity : NacreCraft.Registry.Entity
        => reference?.GetEntity(level);

    //GetLivingEntity/GetPlayer typed helpers for the common owner kinds
    public static LivingEntity? GetLivingEntity(EntityReference<LivingEntity>? reference, ServerLevel level)
        => Get(reference, level);

    public static Player? GetPlayer(EntityReference<Player>? reference, ServerLevel level)
        => Get(reference, level);
}

//OwnableEntity an entity with an owner reference, maps to vanilla net.minecraft.world.entity.OwnableEntity
public interface OwnableEntity
{
    EntityReference<LivingEntity>? GetOwnerReference();

    LivingEntity? GetOwner();

    //GetRootOwner the top of an ownership chain, null when the chain loops
    LivingEntity? GetRootOwner()
    {
        if (this is not LivingEntity) return null;
        var visited = new HashSet<object>();
        var owner = GetOwner();
        visited.Add(this);
        while (owner is OwnableEntity ownable)
        {
            if (!visited.Add(owner)) return null;
            owner = ownable.GetOwner();
        }
        return owner;
    }
}
