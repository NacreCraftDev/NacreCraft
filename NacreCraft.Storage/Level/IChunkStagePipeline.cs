using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Storage;

//Drives one chunk through the status chain one stage at a time, so a worker is not held for the whole chain
public interface IChunkStagePipeline
{
    //CreateChunk builds the empty chunk and the per-chunk state the later stages need
    ChunkAccess CreateChunk(ChunkPos pos);

    //RunStage executes one status for the chunk
    void RunStage(ChunkAccess chunk, ChunkStatus status);

    //NextStatus returns the following status, null when the chain is complete
    ChunkStatus? NextStatus(ChunkStatus status);

    //CanRunStage whether the neighbours this status reads are already far enough along
    //False parks the chunk instead of spending a worker on a stage that would read half-built neighbours
    bool CanRunStage(ChunkAccess chunk, ChunkStatus status) => true;

    //SetLightInitializer hands the pipeline the light build, so the LIGHT stage runs it in the chain like vanilla
    //The implementation may refuse (returning false), in which case the chunk falls back to the queued light thread
    void SetLightInitializer(Func<ChunkAccess, bool>? initializer) { }
}
