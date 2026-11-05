using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//ChunkStatusUpdateListener receives a chunk's load tier change, maps to vanilla net.minecraft.world.level.entity.ChunkStatusUpdateListener
public delegate void ChunkStatusUpdateListener(ChunkPos pos, FullChunkStatus fullChunkStatus);
