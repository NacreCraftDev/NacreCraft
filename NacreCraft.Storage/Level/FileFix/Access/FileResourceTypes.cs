using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;

namespace NacreCraft.Storage;

//FileResourceTypes the built-in file kinds, maps to vanilla net.minecraft.util.filefix.access.FileResourceTypes
public static class FileResourceTypes
{
    public static readonly FileResourceType<LevelDat> LEVEL_DAT =
        new((path, dataVersion, fixer) => new LevelDat(path, dataVersion, fixer));

    public static readonly FileResourceType<PlayerData> PLAYER_DATA =
        new((path, dataVersion, fixer) => new PlayerData(path, dataVersion, fixer));

    public static FileResourceType<SavedDataNbt> SavedData(DSL.ITypeReference type)
        => SavedData(type, CompressedNbt.MissingSeverity.NEUTRAL);

    public static FileResourceType<SavedDataNbt> SavedData(DSL.ITypeReference type, CompressedNbt.MissingSeverity missingSeverity)
        => new((path, dataVersion, fixer) => new SavedDataNbt(type, path, dataVersion, missingSeverity, fixer));

    public static FileResourceType<ChunkNbt> Chunk(DataFixTypes type, RegionStorageInfo info)
        => new((path, dataVersion, fixer) => new ChunkNbt(info, path, type, dataVersion, fixer));
}
