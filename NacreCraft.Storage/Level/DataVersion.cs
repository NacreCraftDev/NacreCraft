using NacreCraft.Config;

namespace NacreCraft.Storage;

//DataVersion maps to vanilla net.minecraft.world.level.storage.DataVersion
//Holds the data version number and its series; worlds from different series are incompatible
public sealed record DataVersion(int Version, string Series)
{
    //MainSeries, the release series key, maps to vanilla MAIN_SERIES
    public const string MainSeries = "main";

    //Current, the data version of this build, maps to SharedConstants.getCurrentVersion().dataVersion()
    public static readonly DataVersion Current = new(SharedConstants.WorldDataVersion, MainSeries);

    //IsSideSeries reports whether this version does not belong to the main series
    public bool IsSideSeries() => !string.Equals(Series, MainSeries, StringComparison.Ordinal);

    //IsCompatible checks the series only, maps to vanilla isCompatible
    //Degraded: SharedConstants.DEBUG_OPEN_INCOMPATIBLE_WORLDS has no counterpart in this project, so the debug bypass is absent
    public bool IsCompatible(DataVersion other) => string.Equals(Series, other.Series, StringComparison.Ordinal);
}
