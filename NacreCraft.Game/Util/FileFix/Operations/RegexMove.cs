using System.Text.RegularExpressions;
using NacreCraft.Game.Util.WorldUpdate;

namespace NacreCraft.Game.Util.FileFix;

//RegexMove moves every entry whose name matches the pattern, maps to vanilla net.minecraft.util.filefix.operations.RegexMove
public sealed record RegexMove(Regex FromPattern, string ToReplacement) : FileFixOperation
{
    public RegexMove(string fromPattern, string toPattern) : this(new Regex(fromPattern), toPattern)
    {
    }

    public void Fix(IWorldFileSystem fileSystem, UpgradeProgress upgradeProgress)
    {
        if (!fileSystem.Exists(string.Empty) || !fileSystem.IsDirectory(string.Empty)) return;
        foreach (var fileName in fileSystem.List(string.Empty))
        {
            //Java matches() is a whole-string match, not a find
            var match = FromPattern.Match(fileName);
            if (!match.Success || match.Index != 0 || match.Length != fileName.Length) continue;
            var newName = FromPattern.Replace(fileName, ToReplacement);
            FileFixUtil.MoveFile(fileSystem, fileName, newName);
        }
    }
}
