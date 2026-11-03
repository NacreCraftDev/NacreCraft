using NacreCraft;
using NacreCraft.Util;
using NacreCraft.Util.Tracing;

namespace NacreCraft.Client.Profiling;

//ClientProfiler starts and stops the client runtime trace
//maps to the role of net.minecraft.client.profiling.ClientMetricsSamplersProvider, replaced here by an EventPipe capture
public static class ClientProfiler
{
    public static bool IsRunning => RuntimeTraceRecorder.IsRunning;

    //Start begins a capture under the traces directory, returns the file being written
    public static string? Start() => RuntimeTraceRecorder.Start(AppPaths.TracesDir, new[] { ClientTrace.Provider });

    //Stop ends the capture and returns the file it wrote
    public static string? Stop() => RuntimeTraceRecorder.Stop();
}
