using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//PlayerStorageFileFix moves the legacy player folders under players/, maps to vanilla net.minecraft.util.filefix.fixes.PlayerStorageFileFix
public sealed class PlayerStorageFileFix : FileFix
{
    public PlayerStorageFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
    {
        AddFileFixOperation(FileFixOperations.Move("advancements", "players/advancements"));
        AddFileFixOperation(FileFixOperations.Move("playerdata", "players/data"));
        AddFileFixOperation(FileFixOperations.Move("stats", "players/stats"));
    }
}
