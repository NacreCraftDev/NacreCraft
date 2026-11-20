namespace NacreCraft.Client.Blaze3d;

//TracyBootstrap would load the Tracy profiler native client, maps to vanilla com.mojang.blaze3d.TracyBootstrap
//NacreCraft ships no Tracy native and captures profiles through EventPipe instead, so this stays a no-op that keeps the
//ported call sites source-compatible
public static class TracyBootstrap
{
    private static bool _setup;

    public static void Setup() => _setup = true;

    public static bool IsSetup => _setup;
}
