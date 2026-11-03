using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Util.Debug;

//DebugValueAccess read access to synchronized debug values, maps to vanilla net.minecraft.util.debug.DebugValueAccess
public interface DebugValueAccess
{
    void ForEachChunk<T>(DebugSubscription<T> subscription, Action<ChunkPos, T> consumer);

    T? GetChunkValue<T>(DebugSubscription<T> subscription, ChunkPos pos);

    void ForEachBlock<T>(DebugSubscription<T> subscription, Action<BlockPos, T> consumer);

    T? GetBlockValue<T>(DebugSubscription<T> subscription, BlockPos pos);

    void ForEachEntity<T>(DebugSubscription<T> subscription, Action<NacreCraft.Registry.Entity, T> consumer);

    T? GetEntityValue<T>(DebugSubscription<T> subscription, NacreCraft.Registry.Entity entity);

    void ForEachEvent<T>(DebugSubscription<T> subscription, EventVisitor<T> visitor);

    //EventVisitor receives an event value plus the chunk coordinates it happened in, maps to vanilla DebugValueAccess.EventVisitor
    public interface EventVisitor<T>
    {
        void Accept(T value, int chunkX, int chunkZ);
    }
}
