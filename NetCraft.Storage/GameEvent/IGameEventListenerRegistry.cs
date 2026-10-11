namespace NetCraft.Storage;

//IGameEventListenerRegistry opaque slot for the game event listener registry of one chunk section
//The registry itself listens for game events and needs the world and the event system, both of which live in the Game
//layer; Storage only needs a place to keep one per section, so the interface stays empty and the Game layer fills it
public interface IGameEventListenerRegistry
{
}
