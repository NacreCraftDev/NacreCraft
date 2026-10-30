namespace NacreCraft.Util;

//Interrupt handling before the logger and the server exist; a real handler replaces the early one later
public static class ConsoleInterrupt
{
    private static readonly object Gate = new();
    private static ConsoleCancelEventHandler? _handler;

    //InstallEarly prints a plain marker and exits, because there is no logger or server to stop yet
    public static void InstallEarly()
    {
        SetHandler((_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("ServerStop");
            Console.Out.Flush();
            Environment.Exit(0);
        });
    }

    //Replace swaps the handler owned by this class so the early one does not fire alongside the real one
    public static void Replace(ConsoleCancelEventHandler handler)
    {
        SetHandler(handler);
    }

    private static void SetHandler(ConsoleCancelEventHandler handler)
    {
        lock (Gate)
        {
            if (_handler is not null) Console.CancelKeyPress -= _handler;
            _handler = handler;
            Console.CancelKeyPress += handler;
        }
    }
}
