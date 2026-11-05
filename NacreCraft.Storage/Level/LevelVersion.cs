using NacreCraft.Codec;
using NacreCraft.Config;
using NacreCraft.Nbt;

namespace NacreCraft.Storage;

//LevelVersion maps to vanilla net.minecraft.world.level.storage.LevelVersion
//Reads the version block of level.dat: storage version, last played time and the game version that wrote it
public sealed class LevelVersion
{
    private LevelVersion(int levelDataVersion, long lastPlayed, string minecraftVersionName, int minecraftVersion, string series, bool snapshot)
    {
        LevelDataVersion = levelDataVersion;
        LastPlayed = lastPlayed;
        MinecraftVersionName = minecraftVersionName;
        MinecraftVersion = new DataVersion(minecraftVersion, series);
        Snapshot = snapshot;
    }

    //LevelDataVersion, the "version" field, 19133 for the Anvil format
    public int LevelDataVersion { get; }

    //LastPlayed, the "LastPlayed" field in epoch milliseconds
    public long LastPlayed { get; }

    //MinecraftVersionName, the writing game version name from the "Version" block
    public string MinecraftVersionName { get; }

    //MinecraftVersion, the "Id" and "Series" fields of the "Version" block
    public DataVersion MinecraftVersion { get; }

    //Snapshot, whether the writing version was a snapshot
    public bool Snapshot { get; }

    //Parse reads a level.dat Data layer, maps to vanilla LevelVersion.parse
    //Degraded: the snapshot default uses false because SharedConstants has no stable flag, vanilla uses !currentVersion.stable()
    public static LevelVersion Parse(Dynamic<Tag> input)
    {
        var levelDataVersion = input.Get("version").AsInt(0);
        var lastPlayed = input.Get("LastPlayed").AsLong(0L);
        var optionalVersion = input.Get("Version").Result();
        if (optionalVersion.IsPresent)
        {
            var version = optionalVersion.Get();
            return new LevelVersion(
                levelDataVersion,
                lastPlayed,
                version.Get("Name").AsString(SharedConstants.Version),
                version.Get("Id").AsInt(SharedConstants.WorldDataVersion),
                version.Get("Series").AsString(DataVersion.MainSeries),
                version.Get("Snapshot").AsBoolean(false));
        }
        return new LevelVersion(levelDataVersion, lastPlayed, string.Empty, 0, DataVersion.MainSeries, false);
    }
}
