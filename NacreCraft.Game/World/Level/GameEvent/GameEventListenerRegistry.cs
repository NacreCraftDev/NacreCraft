using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.GameEvents;

//GameEventListenerRegistry holds the listeners of one chunk section, maps to vanilla GameEventListenerRegistry
public interface GameEventListenerRegistry
{
    //Noop registry that holds nothing, used by chunk sections with no listeners
    public static readonly GameEventListenerRegistry Noop = new NoopGameEventListenerRegistry();

    //IsEmpty whether no listener is registered
    bool IsEmpty { get; }

    //Register adds a listener
    void Register(GameEventListener listener);

    //Unregister removes a listener
    void Unregister(GameEventListener listener);

    //VisitInRangeListeners offers the event to every listener within range
    //The return value says whether any listener was close enough to count as applicable
    bool VisitInRangeListeners(GameEvent gameEvent, Vec3 sourcePosition, GameEvent.Context context, ListenerVisitor visitor);
}

//ListenerVisitor receives one in-range listener together with its position, maps to vanilla ListenerVisitor
public delegate void ListenerVisitor(GameEventListener listener, Vec3 position);

//NoopGameEventListenerRegistry registry that keeps nothing and reports no applicable listener
internal sealed class NoopGameEventListenerRegistry : GameEventListenerRegistry, IGameEventListenerRegistry
{
    public bool IsEmpty => true;

    public void Register(GameEventListener listener) { }

    public void Unregister(GameEventListener listener) { }

    public bool VisitInRangeListeners(GameEvent gameEvent, Vec3 sourcePosition, GameEvent.Context context, ListenerVisitor visitor)
        => false;
}
