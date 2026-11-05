using NacreCraft.Registry;

namespace NacreCraft.Storage;

//EntityTickList the set of entities that tick this frame, maps to vanilla net.minecraft.world.level.entity.EntityTickList
//The active/passive swap lets a tick modify the list while it is being iterated
public sealed class EntityTickList
{
    private Dictionary<int, Entity> _active = new();
    private Dictionary<int, Entity> _passive = new();
    private Dictionary<int, Entity>? _iterated;

    private void EnsureActiveIsNotIterated()
    {
        if (!ReferenceEquals(_iterated, _active)) return;
        _passive.Clear();
        foreach (var (id, entity) in _active) _passive[id] = entity;
        (_active, _passive) = (_passive, _active);
    }

    public void Add(Entity entity)
    {
        EnsureActiveIsNotIterated();
        _active[entity.EntityId] = entity;
    }

    public void Remove(Entity entity)
    {
        EnsureActiveIsNotIterated();
        _active.Remove(entity.EntityId);
    }

    public bool Contains(Entity entity) => _active.ContainsKey(entity.EntityId);

    //ForEach visits every active entity; nested iteration is refused like vanilla
    public void ForEach(Action<Entity> output)
    {
        if (_iterated is not null) throw new InvalidOperationException("Only one concurrent iteration supported");
        _iterated = _active;
        try
        {
            foreach (var entity in _active.Values) output(entity);
        }
        finally
        {
            _iterated = null;
        }
    }
}
