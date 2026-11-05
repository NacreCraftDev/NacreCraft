namespace NacreCraft.Storage.Paletted;

//PalettedContainerRO read-only view of a paletted container, maps to vanilla net.minecraft.world.level.chunk.PalettedContainerRO
//Callers that only sample block states or count contents depend on this interface instead of the mutable container
public interface PalettedContainerRO<T>
{
    T Get(int x, int y, int z);

    void GetAll(Action<T> consumer);

    int BitsPerEntry { get; }

    bool MaybeHas(Predicate<T> predicate);

    void ForEachInPalette(Action<T> consumer);

    void Count(Action<T, int> output);

    PalettedContainer<T> Copy();

    PalettedContainer<T> Recreate();

    PackedData<T> Pack(Strategy<T> strategy);
}
