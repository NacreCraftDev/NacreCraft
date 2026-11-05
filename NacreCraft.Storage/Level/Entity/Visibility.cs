namespace NacreCraft.Storage;

//Visibility how much of a chunk's entities the level exposes, maps to vanilla net.minecraft.world.level.entity.Visibility
public enum Visibility
{
    HIDDEN,
    TRACKED,
    TICKING
}

public static class Visibilities
{
    public static bool IsTicking(this Visibility visibility) => visibility == Visibility.TICKING;

    public static bool IsAccessible(this Visibility visibility) => visibility != Visibility.HIDDEN;

    //FromFullChunkStatus maps a chunk load tier to visibility, maps to vanilla fromFullChunkStatus
    public static Visibility FromFullChunkStatus(FullChunkStatus status)
    {
        if (status.IsOrAfter(FullChunkStatus.EntityTicking)) return Visibility.TICKING;
        if (status.IsOrAfter(FullChunkStatus.Full)) return Visibility.TRACKED;
        return Visibility.HIDDEN;
    }
}
