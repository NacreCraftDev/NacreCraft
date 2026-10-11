using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Storage;

namespace NetCraft.Game.World.Level.GameEvents;

//EuclideanGameEventListenerRegistry listeners of one section filtered by plain block distance
//maps to vanilla EuclideanGameEventListenerRegistry
//Registration and removal are deferred while a dispatch is running: a listener handling an event may add or drop
//listeners itself, and mutating the list mid-iteration would break it
public sealed class EuclideanGameEventListenerRegistry : GameEventListenerRegistry, IGameEventListenerRegistry
{
    private readonly List<GameEventListener> _listeners = new();
    private readonly List<GameEventListener> _listenersToAdd = new();
    private readonly HashSet<GameEventListener> _listenersToRemove = new();

    private readonly ServerLevel _level;
    private readonly int _sectionY;
    private readonly Action<int> _onEmpty;

    private bool _processing;

    public EuclideanGameEventListenerRegistry(ServerLevel level, int sectionY, Action<int> onEmpty)
    {
        _level = level;
        _sectionY = sectionY;
        _onEmpty = onEmpty;
    }

    public bool IsEmpty => _listeners.Count == 0;

    public void Register(GameEventListener listener)
    {
        if (_processing)
            _listenersToAdd.Add(listener);
        else
            _listeners.Add(listener);
    }

    public void Unregister(GameEventListener listener)
    {
        if (_processing)
            _listenersToRemove.Add(listener);
        else
            _listeners.Remove(listener);
        if (_listeners.Count == 0) _onEmpty(_sectionY);
    }

    public bool VisitInRangeListeners(GameEvent gameEvent, Vec3 sourcePosition, GameEvent.Context context, ListenerVisitor visitor)
    {
        _processing = true;
        var applicable = false;
        try
        {
            //Iterate by index: the visitor may reach back into this registry, and a snapshot keeps that safe
            for (var index = 0; index < _listeners.Count; index++)
            {
                var listener = _listeners[index];
                if (_listenersToRemove.Remove(listener)) continue;
                if (PostablePosition(sourcePosition, listener) is not { } listenerPosition) continue;
                visitor(listener, listenerPosition);
                applicable = true;
            }
        }
        finally
        {
            _processing = false;
        }

        if (_listenersToAdd.Count > 0)
        {
            _listeners.AddRange(_listenersToAdd);
            _listenersToAdd.Clear();
        }

        if (_listenersToRemove.Count > 0)
        {
            _listeners.RemoveAll(_listenersToRemove.Contains);
            _listenersToRemove.Clear();
        }

        return applicable;
    }

    //PostablePosition the listener position when the source lies within its radius, otherwise null
    //maps to vanilla getPostableListenerPosition
    private Vec3? PostablePosition(Vec3 sourcePosition, GameEventListener listener)
    {
        if (listener.ListenerSource.GetPosition(_level) is not { } position) return null;

        var radius = listener.ListenerRadius;
        var dx = Math.Floor(position.X) - Math.Floor(sourcePosition.X);
        var dy = Math.Floor(position.Y) - Math.Floor(sourcePosition.Y);
        var dz = Math.Floor(position.Z) - Math.Floor(sourcePosition.Z);
        return dx * dx + dy * dy + dz * dz > radius * radius ? null : position;
    }
}
