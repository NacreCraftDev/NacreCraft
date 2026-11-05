namespace NacreCraft.Game.World.Level.LevelGen.Structure;

//StructureAccess the structure starts and references held by a chunk, maps to vanilla net.minecraft.world.level.chunk.StructureAccess
//Vanilla returns LongSet; NC uses IReadOnlySet<long> for the structure reference positions
public interface StructureAccess
{
    StructureStart? GetStartForStructure(Structure structure);

    void SetStartForStructure(Structure structure, StructureStart start);

    IReadOnlySet<long> GetReferencesForStructure(Structure structure);

    void AddReferenceForStructure(Structure structure, long reference);

    IReadOnlyDictionary<Structure, IReadOnlySet<long>> GetAllReferences();

    void SetAllReferences(IReadOnlyDictionary<Structure, IReadOnlySet<long>> references);
}
