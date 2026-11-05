using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.WorldUpdate;

//FileToUpgrade one region file and the chunks in it that still need upgrading, maps to vanilla net.minecraft.util.worldupdate.FileToUpgrade
public sealed record FileToUpgrade(RegionFile File, IReadOnlyList<ChunkPos> ChunksToUpgrade);
