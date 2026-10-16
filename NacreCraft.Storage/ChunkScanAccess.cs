using NacreCraft.Nbt;
using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//Chunk scan interface, maps to vanilla ChunkScanAccess
//Streams a chunk through a visitor without building a full Tag, for blending and similar scans
public interface ChunkScanAccess
{
    Task ScanChunk(ChunkPos pos, StreamTagVisitor visitor);
}
