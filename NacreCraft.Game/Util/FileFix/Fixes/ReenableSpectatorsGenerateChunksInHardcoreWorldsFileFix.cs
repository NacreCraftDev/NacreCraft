using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;
using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//ReenableSpectatorsGenerateChunksInHardcoreWorldsFileFix restores the gamerule for hardcore worlds, maps to vanilla net.minecraft.util.filefix.fixes.ReenableSpectatorsGenerateChunksInHardcoreWorldsFileFix
public sealed class ReenableSpectatorsGenerateChunksInHardcoreWorldsFileFix : FileFix
{
    public ReenableSpectatorsGenerateChunksInHardcoreWorldsFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
    {
        AddFileContentFix(files =>
        {
            var levelDat = files.GetFileAccess(FileResourceTypes.LEVEL_DAT, FileRelations.ORIGIN.ForFile("level.dat"));
            var gameRules = files.GetFileAccess(FileResourceTypes.SavedData(References.SavedDataGameRules),
                FileRelations.DATA.ForFile("minecraft/game_rules.dat"));
            return upgradeProgress =>
            {
                upgradeProgress.SetType(UpgradeProgress.Type.FILES);
                var levelData = levelDat.GetOnlyFile().Read();
                if (!levelData.IsPresent) return;

                var data = levelData.Get();
                var wasEverLoadedInSingleplayer = data.Get("singleplayer_uuid").Result().IsPresent;
                var hardcore = data.Get("difficulty_settings").Result()
                    .OrElse(data.EmptyMap()).Get("hardcore").AsBoolean(false);
                if (!wasEverLoadedInSingleplayer || !hardcore) return;

                var gameRulesFile = gameRules.GetOnlyFile();
                var rules = gameRulesFile.Read();
                if (rules.IsPresent)
                {
                    gameRulesFile.Write(rules.Get().Update("minecraft:spectators_generate_chunks",
                        old => old.CreateBoolean(true)));
                }
            };
        });
    }
}
