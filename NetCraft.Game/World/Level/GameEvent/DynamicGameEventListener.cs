using NetCraft.Primitives;
using NetCraft.Registry;
using NetCraft.Storage;

namespace NetCraft.Game.World.Level.GameEvents;

//DynamicGameEventListener keeps one listener registered in whichever chunk section it currently sits in
//maps to vanilla DynamicGameEventListener
//Listeners that follow an entity move between sections, so the registry they live in has to follow as well
public sealed class DynamicGameEventListener<T>(T listener) where T : GameEventListener
{
    private SectionPos? _lastSection;

    //Listener the wrapped listener
    public T Listener => listener;

    //Add registers the listener where it currently is, maps to vanilla add
    public void Add(ServerLevel level) => Move(level);

    //Remove unregisters the listener from the section it was last seen in, maps to vanilla remove
    public void Remove(ServerLevel level)
        => IfChunkExists(level, _lastSection, registry => registry.Unregister(listener));

    //Move re-registers the listener once it crosses into another section, maps to vanilla move
    //An unresolved source is skipped: the listener simply stays where it was
    public void Move(ServerLevel level)
    {
        if (listener.ListenerSource.GetPosition(level) is not { } position) return;

        var current = SectionPos.Of(
            SectionPos.BlockToSectionCoord(position.X),
            SectionPos.BlockToSectionCoord(position.Y),
            SectionPos.BlockToSectionCoord(position.Z));

        if (_lastSection is { } last && last.Equals(current)) return;

        IfChunkExists(level, _lastSection, registry => registry.Unregister(listener));
        _lastSection = current;
        IfChunkExists(level, _lastSection, registry => registry.Register(listener));
    }

    //IfChunkExists runs the action against the section registry, doing nothing when the section or its chunk is absent
    //maps to vanilla ifChunkExists
    private static void IfChunkExists(ServerLevel level, SectionPos? section, Action<GameEventListenerRegistry> action)
    {
        if (section is not { } pos) return;
        if (level.GetChunk(new ChunkPos(pos.X, pos.Z)) is not { } chunk) return;
        if (chunk.GetListenerRegistry(pos.Y) is GameEventListenerRegistry registry) action(registry);
    }
}
