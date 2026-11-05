using NacreCraft.Primitives;
using NacreCraft.Registry.Flag;
using NacreCraft.Storage;
using NacreCraft.Storage.Redstone;

namespace NacreCraft.Game.World.Level.Redstone;

//ExperimentalRedstoneUtils helpers for the redstone-experiments frame, maps to vanilla net.minecraft.world.level.redstone.ExperimentalRedstoneUtils
public static class ExperimentalRedstoneUtils
{
    //InitialOrientation the starting frame, null when the level did not enable the experiments, maps to vanilla initialOrientation
    public static Orientation? InitialOrientation(ServerLevel level, Direction? front, Direction? up)
    {
        if (!level.EnabledFeatures.Contains(FeatureFlags.REDSTONE_EXPERIMENTS)) return null;
        var orientation = Orientation.Random(level.Random).WithSideBias(Orientation.SideBias.LEFT);
        if (up is { } upDirection) orientation = orientation.WithUp(upDirection);
        if (front is { } frontDirection) orientation = orientation.WithFront(frontDirection);
        return orientation;
    }

    //WithFront changes the front when a frame exists, maps to vanilla withFront
    public static Orientation? WithFront(Orientation? orientation, Direction front)
        => orientation?.WithFront(front);
}
