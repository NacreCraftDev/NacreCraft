namespace NacreCraft.Storage;

//ColumnPos a horizontal block column position, maps to vanilla net.minecraft.server.level.ColumnPos
//Packs the two 32-bit coordinates into one long for map keys
public readonly record struct ColumnPos(int X, int Z)
{
    //ToLong packs the coordinates, maps to vanilla ColumnPos.asLong
    public long ToLong() => ((long)X << 32) | (uint)Z;

    //FromLong unpacks, maps to vanilla ColumnPos.fromLong
    public static ColumnPos FromLong(long packed) => new((int)(packed >> 32), (int)packed);

    public override string ToString() => $"[{X}, {Z}]";
}
