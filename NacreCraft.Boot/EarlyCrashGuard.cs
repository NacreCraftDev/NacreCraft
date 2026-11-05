using System.Text;
using NacreCraft.Logging;

namespace NacreCraft.Boot;

//EarlyCrashGuard covers the window before the kernel installs its own crash handler
//It only touches the BCL, so it can be armed on the first line of an entry point, when loading any kernel assembly
//would close the injection window. Once the real handler is in place this one steps aside.
public static class EarlyCrashGuard
{
    private static bool _armed;
    private static volatile bool _disabled;

    //Install subscribes the BCL-only handlers; idempotent
    public static void Install()
    {
        if (_armed) return;
        _armed = true;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Log.Debug("Early crash guard armed");
    }

    //Disable hands the job over to the kernel handler; called from that handler's own install
    public static void Disable() => _disabled = true;

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (_disabled) return;
        if (e.ExceptionObject is Exception exception) Write("unhandled", exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (_disabled) return;
        e.SetObserved();
        Write("unobserved-task", e.Exception);
    }

    private static void Write(string kind, Exception exception)
    {
        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "crash-reports");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"early-{kind}-{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.txt");

            var text = new StringBuilder();
            text.AppendLine($"early crash report ({kind})");
            text.AppendLine($"time: {DateTime.Now:O}");
            text.AppendLine($"process: {Environment.ProcessPath}");
            text.AppendLine($"thread: {Environment.CurrentManagedThreadId}");
            text.AppendLine();
            text.AppendLine(exception.ToString());

            File.WriteAllText(path, text.ToString());
            Log.Critical($"Early crash report written to {path}");
        }
        catch
        {
            //The process is already going down; a report that cannot be written must not make it worse
        }
    }
}
