using NacreCraft.Game.Util.WorldUpdate;

namespace NacreCraft.Game.Util.FileFix;

//GroupMove applies the same set of moves for every source-to-target folder pair, maps to vanilla net.minecraft.util.filefix.operations.GroupMove
public sealed record GroupMove(IReadOnlyDictionary<string, string> FromTo, IReadOnlyList<Move> Fixers) : FileFixOperation
{
    public void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
    {
        foreach (var (from, to) in FromTo)
            foreach (var moveOperation in Fixers)
                moveOperation.Relative(from, to).Fix(fileSystem, upgradeProgress);
    }
}
