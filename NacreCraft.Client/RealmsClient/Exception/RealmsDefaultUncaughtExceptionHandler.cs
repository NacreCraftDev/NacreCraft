using NacreCraft.Logging;

namespace NacreCraft.Client.RealmsClient.Exception;

//RealmsDefaultUncaughtExceptionHandler logs uncaught background exceptions
//maps to com.mojang.realmsclient.exception.RealmsDefaultUncaughtExceptionHandler
//C# threads have no per-thread uncaught handler, so the caller wires this into its own handler
public sealed class RealmsDefaultUncaughtExceptionHandler
{
    public void UncaughtException(System.Exception exception)
        => Log.Error($"Caught previously unhandled exception: {exception}");
}
