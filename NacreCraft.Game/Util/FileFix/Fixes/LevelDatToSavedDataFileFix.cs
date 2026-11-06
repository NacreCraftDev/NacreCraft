using System.Collections.Generic;
using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;
using NacreCraft.Game.Util.WorldUpdate;
using NacreCraft.Nbt;
using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix.Fixes;

//LevelDatToSavedDataFileFix moves world state out of level.dat into per-file saved data, maps to vanilla net.minecraft.util.filefix.fixes.LevelDatToSavedDataFileFix
public sealed class LevelDatToSavedDataFileFix : FileFix
{
    private const string Overworld = "overworld";
    private const string TheNether = "the_nether";
    private const string TheEnd = "the_end";
    private const string WorldBorderKey = "world_border";
    private const string WorldBorderFileName = "minecraft/world_border.dat";

    public LevelDatToSavedDataFileFix(Schema schema, NacreCraft.DataFixer.DataFixer fixer)
        : base(schema, fixer)
    {
    }

    public override void MakeFixer()
    {
        AddFileContentFix(files =>
        {
            var levelDat = files.GetFileAccess(FileResourceTypes.LEVEL_DAT, FileRelations.ORIGIN.ForFile("level.dat"));
            var dragonFight = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataEnderDragonFight),
                FileRelations.ForDataFileInDimension(TheEnd, "minecraft/ender_dragon_fight.dat"));
            var fallbackPlayerData = files.GetFileAccess(FileResourceTypes.PLAYER_DATA,
                FileRelations.PLAYER_DATA.ForFile(Guid.Empty + ".dat"));
            var wanderingTrader = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWanderingTrader),
                FileRelations.DATA.ForFile("minecraft/wandering_trader.dat"));
            var customBossEvents = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataCustomBossEvents),
                FileRelations.DATA.ForFile("minecraft/custom_boss_events.dat"));
            var weatherData = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWeather),
                FileRelations.DATA.ForFile("minecraft/weather.dat"));
            var scheduledEvents = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataScheduledEvents),
                FileRelations.DATA.ForFile("minecraft/scheduled_events.dat"));
            var worldBorderOverworld = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWorldBorder),
                FileRelations.ForDataFileInDimension(Overworld, WorldBorderFileName));
            var worldBorderNether = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWorldBorder),
                FileRelations.ForDataFileInDimension(TheNether, WorldBorderFileName));
            var worldBorderEnd = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWorldBorder),
                FileRelations.ForDataFileInDimension(TheEnd, WorldBorderFileName));
            var gameRules = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataGameRules),
                FileRelations.DATA.ForFile("minecraft/game_rules.dat"));
            var worldGenSettings = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWorldGenSettings),
                FileRelations.DATA.ForFile("minecraft/world_gen_settings.dat"));
            var worldClocks = files.GetFileAccess(
                FileResourceTypes.SavedData(References.SavedDataWorldClocks),
                FileRelations.DATA.ForFile("minecraft/world_clocks.dat"));

            return upgradeProgress =>
            {
                upgradeProgress.SetType(UpgradeProgress.Type.FILES);
                var levelDatFile = levelDat.GetOnlyFile();
                var readData = levelDatFile.Read();
                if (!readData.IsPresent) return;

                var content = readData.Get();
                content = ExtractToFile(dragonFight, content, "dragon_fight");
                content = ExtractPlayerDataToFile(files.Fixer, fallbackPlayerData, content);
                content = ExtractToFile(wanderingTrader, content, "wandering_trader_migration_data");
                content = ExtractToFile(customBossEvents, content, "CustomBossEvents");
                content = ExtractToFile(weatherData, content, "weather_data");
                content = ExtractToFile(scheduledEvents, content, "scheduled_events");
                content = ExtractWorldBorderToFiles(worldBorderOverworld, worldBorderNether, worldBorderEnd, content);
                content = ExtractToFile(gameRules, content, "game_rules");
                content = ExtractWorldGenSettingsToFile(files.Fixer, worldGenSettings, content);
                content = ExtractToFile(worldClocks, content, "world_clocks");
                levelDatFile.Write(content);
            };
        });
    }

    private static Dynamic<Tag> ExtractToFile<T>(FileAccess<T> targetFile, Dynamic<Tag> content, string key)
        where T : CompressedNbt
    {
        var tag = content.Get(key).Result();
        if (!tag.IsPresent) return content;
        targetFile.GetOnlyFile().Write(tag.Get());
        return content.Remove(key);
    }

    private Dynamic<Tag> ExtractPlayerDataToFile(DataFixer.DataFixer fixer, FileAccess<PlayerData> fallbackFile,
        Dynamic<Tag> content)
    {
        var playerTag = content.Get("Player").Result();
        if (!playerTag.IsPresent) return content;

        var playerTagFixed = DataFixTypes.Player.Update(fixer, playerTag.Get(),
            NbtUtils.GetDataVersion(playerTag.Get()), GetVersion());
        var playerUuid = playerTagFixed.Get("UUID").Result();
        if (playerUuid.IsPresent)
            return content.Remove("Player").Set("singleplayer_uuid", playerUuid.Get());

        fallbackFile.GetOnlyFile().Write(playerTagFixed);
        var zeroUuid = content.CreateList(Enumerable.Range(0, 4).Select(_ => content.CreateInt(0)));
        return content.Remove("Player").Set("singleplayer_uuid", zeroUuid);
    }

    private static Dynamic<Tag> ExtractWorldBorderToFiles(FileAccess<SavedDataNbt> overworld,
        FileAccess<SavedDataNbt> nether, FileAccess<SavedDataNbt> end, Dynamic<Tag> content)
    {
        ExtractWorldBorderToFile(overworld, content, 1.0);
        ExtractWorldBorderToFile(nether, content, 8.0);
        ExtractWorldBorderToFile(end, content, 1.0);
        return content.Remove(WorldBorderKey);
    }

    private static void ExtractWorldBorderToFile(FileAccess<SavedDataNbt> targetFile, Dynamic<Tag> content, double divider)
    {
        var worldBorderTag = content.Get(WorldBorderKey).Result();
        if (!worldBorderTag.IsPresent) return;

        var scaled = worldBorderTag.Get()
            .Update("center_x", x => x.CreateDouble(x.AsDouble(0.0) / divider))
            .Update("center_z", z => z.CreateDouble(z.AsDouble(0.0) / divider));
        targetFile.GetOnlyFile().Write(scaled);
    }

    private Dynamic<Tag> ExtractWorldGenSettingsToFile(DataFixer.DataFixer fixer,
        FileAccess<SavedDataNbt> targetFile, Dynamic<Tag> content)
    {
        var worldGenSettingsTag = content.Get("world_gen_settings").Result();
        if (!worldGenSettingsTag.IsPresent) return content;

        var fixedTag = DataFixTypes.WorldGenSettings.Update(fixer, worldGenSettingsTag.Get(),
            NbtUtils.GetDataVersion(content), GetVersion());
        targetFile.GetOnlyFile().Write(fixedTag);
        return content.Remove("world_gen_settings");
    }
}
