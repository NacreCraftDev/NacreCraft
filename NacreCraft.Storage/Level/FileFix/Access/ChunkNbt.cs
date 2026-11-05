using NacreCraft.DataFixer;
using NacreCraft.Nbt;
using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//ChunkNbt one region file holding chunk data, maps to vanilla net.minecraft.util.filefix.access.ChunkNbt
public sealed class ChunkNbt : IDisposable
{
    private readonly SimpleRegionStorage _storage;
    private readonly int _targetVersion;

    public ChunkNbt(RegionStorageInfo info, string path, DataFixTypes type, int targetVersion, NacreCraft.DataFixer.DataFixer fixer)
    {
        _targetVersion = targetVersion;
        _storage = new SimpleRegionStorage(info, path, fixer, false, type);
    }

    //UpdateChunk upgrades and rewrites one chunk, maps to vanilla updateChunk
    public async Task UpdateChunk(ChunkPos pos, CompoundTag dataFixContext, Func<CompoundTag, CompoundTag> fixer)
    {
        var maybeTag = await _storage.Read(pos).ConfigureAwait(false);
        if (!maybeTag.IsPresent) return;
        var tag = _storage.UpgradeChunkTag(maybeTag.Get(), -1, dataFixContext, _targetVersion);
        tag = fixer(tag);
        await _storage.Write(pos, tag).ConfigureAwait(false);
    }

    public void Dispose() => _storage.Dispose();
}
