namespace NacreCraft.Config;

//BuildInfo version and build stamps of the running assembly
//The number and the compile time are written into a generated partial on every build; this half is written by hand so the
//release version lives in source control and only changes when someone bumps it
public static partial class BuildInfo
{
    //GameVersion release version this build belongs to
    public const string GameVersion = "1.0";

    //Display "version.build", the form the startup banner prints
    public static string Display => $"{GameVersion}.{BuildNumber}";
}
