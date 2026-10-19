using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Server.Progress;

//ChunkLoadStatusView a status window over the chunks around one centre, maps to vanilla ChunkLoadStatusView
public interface ChunkLoadStatusView
{
    //MoveTo sets the dimension and centre chunk
    void MoveTo(ResourceKey<Level> dimension, ChunkPos chunkPos);

    //Get the status at an offset from the centre, null when no holder exists
    ChunkStatus? Get(int x, int z);

    //Radius the window radius in chunks
    int Radius { get; }
}
