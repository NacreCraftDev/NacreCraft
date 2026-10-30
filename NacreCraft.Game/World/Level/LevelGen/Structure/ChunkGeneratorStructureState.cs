using NacreCraft.Registry;

namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//ChunkGeneratorStructureState structure generation state, maps to vanilla net.minecraft.world.level.chunk.ChunkGeneratorStructureState
//Holds the world seed and structure set list for placement judges and exclusion-zone queries
//Keeps only what the judge needs; concentric-ring preset positions will be added when strongholds are wired up
public sealed class ChunkGeneratorStructureState
{
    //LevelSeed world seed; all placement judges derive from it
    public long LevelSeed { get; }

    public ChunkGeneratorStructureState(long levelSeed) => LevelSeed = levelSeed;

    //HasStructureChunkInRange whether the target set has a placement point within range, maps to vanilla hasStructureChunkInRange
    //Asks the target set's placement chunk by chunk; the sole basis for exclusion-zone checks
    public bool HasStructureChunkInRange(Holder<NacreCraft.Registry.StructureSet> structureSet,
        int sourceX, int sourceZ, int range)
    {
        if (!structureSet.IsBound() || structureSet.Value is not StructureSet set) return false;
        //Delegated to the placement so repeated exclusion-zone queries hit its per-coordinate memo
        return set.Placement.HasStructureChunkInRange(this, sourceX, sourceZ, range);
    }
}
