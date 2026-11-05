namespace NacreCraft.Storage;

//LevelResource maps to vanilla net.minecraft.world.level.storage.LevelResource
//Names a file or directory inside a world folder; the instances below cover every path the storage layer asks for
public sealed record LevelResource(string Id)
{
    public static readonly LevelResource PlayerAdvancementsDir = new("players/advancements");
    public static readonly LevelResource PlayerStatsDir = new("players/stats");
    public static readonly LevelResource PlayerDataDir = new("players/data");
    public static readonly LevelResource PlayerOldDataDir = new("players");
    public static readonly LevelResource LevelDataFile = new("level.dat");
    public static readonly LevelResource OldLevelDataFile = new("level.dat_old");
    public static readonly LevelResource IconFile = new("icon.png");
    public static readonly LevelResource LockFile = new(DirectoryLock.LockFileName);
    public static readonly LevelResource GeneratedDir = new("generated");
    public static readonly LevelResource DatapackDir = new("datapacks");
    public static readonly LevelResource MapResourceFile = new("resourcepacks/resources.zip");
    public static readonly LevelResource Data = new("data");
    public static readonly LevelResource Root = new(".");

    public override string ToString() => "/" + Id;
}
