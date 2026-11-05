namespace NacreCraft.Storage;

//RespawnData maps to the vanilla nested record net.minecraft.world.level.storage.LevelData.RespawnData
//Degraded: GlobalPos is not ported into the Storage layer, so the dimension id and the block position are kept as flat fields
public sealed record RespawnData(string Dimension, int X, int Y, int Z, float Yaw, float Pitch)
{
    //OverworldDimension, the default dimension id, maps to vanilla Level.OVERWORLD
    public const string OverworldDimension = "minecraft:overworld";

    //Default, the zero spawn point in the overworld, maps to vanilla RespawnData.DEFAULT
    public static readonly RespawnData Default = new(OverworldDimension, 0, 0, 0, 0f, 0f);

    //Of builds a respawn point with the same clamps as vanilla RespawnData.of
    public static RespawnData Of(string dimension, int x, int y, int z, float yaw, float pitch)
        => new(dimension, x, y, z, WrapDegrees(yaw), Math.Clamp(pitch, -90f, 90f));

    //WrapDegrees folds the yaw into [-180,180], maps to vanilla Mth.wrapDegrees
    private static float WrapDegrees(float degrees)
    {
        var wrapped = degrees % 360f;
        if (wrapped >= 180f) wrapped -= 360f;
        if (wrapped < -180f) wrapped += 360f;
        return wrapped;
    }
}
