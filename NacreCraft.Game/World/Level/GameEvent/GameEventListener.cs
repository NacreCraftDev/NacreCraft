using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.GameEvents;

//GameEventListener receives game events that happen near it, maps to vanilla GameEventListener
public interface GameEventListener
{
    //ListenerSource where this listener sits, resolved per level because it may follow an entity
    PositionSource ListenerSource { get; }

    //ListenerRadius how far this listener reaches, in blocks
    int ListenerRadius { get; }

    //HandleGameEvent handles one event; returning false is not an error, it only means the event did not apply
    bool HandleGameEvent(NacreCraft.Storage.ServerLevel level, GameEvent gameEvent, GameEvent.Context context, Vec3 position);

    //DeliveryMode how this listener wants events delivered, maps to vanilla getDeliveryMode
    //BY_DISTANCE defers the call so the nearest listener is served first; the default happens inline
    DeliveryMode Mode => DeliveryMode.Unspecified;
}

//DeliveryMode delivery ordering of a listener, maps to vanilla GameEventListener.DeliveryMode
public enum DeliveryMode
{
    Unspecified,

    ByDistance
}

//GameEventListenerProvider supplies a listener, maps to vanilla GameEventListener.Provider
//Lets the registry hold something that has not been built yet
public interface GameEventListenerProvider<out T> where T : GameEventListener
{
    T Listener { get; }
}
