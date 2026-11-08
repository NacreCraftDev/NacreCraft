using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using NacreCraft.Logging;
using NacreCraft.Util;

namespace NacreCraft.Game.Server;

//ServerWatchdog kills the process when one server tick runs longer than max-tick-time, maps to vanilla net.minecraft.server.dedicated.ServerWatchdog
//The main loop publishes the target time of its next tick; the watchdog compares it against the live tick clock and reports a crash
//once the difference passes the threshold. Vanilla dumps every ThreadInfo; NC has no whole-process managed thread dump, so the report
//carries the operating-system thread table plus the watchdog stack, which is what distinguishes "main thread stuck" from "process starved"
public sealed class ServerWatchdog
{
    //MaxShutdownTimeMillis how long a graceful exit gets before the process is halted, maps to vanilla MAX_SHUTDOWN_TIME
    private const long MaxShutdownTimeMillis = 10_000;

    //ShutdownStatus the exit code of a watchdog shutdown, maps to vanilla SHUTDOWN_STATUS
    private const int ShutdownStatus = 1;

    private readonly DedicatedServer _server;
    private readonly long _maxTickTimeMillis;
    private readonly Thread _thread;

    //_stopped the loop exits on a graceful shutdown without reporting a crash
    private volatile bool _stopped;

    public ServerWatchdog(DedicatedServer server)
    {
        _server = server;
        _maxTickTimeMillis = Math.Max(1, server.MaxTickLength);
        _thread = new Thread(Run) { Name = "Server Watchdog", IsBackground = true };
    }

    //Start starts the watchdog thread, idempotent
    public void Start()
    {
        if (!_thread.IsAlive) _thread.Start();
    }

    //Stop asks the watchdog to leave its loop, called by the server shutdown path before the process exits normally
    public void Stop() => _stopped = true;

    private void Run()
    {
        Log.Info($"Server watchdog started, max tick time {_maxTickTimeMillis}ms");
        while (!_stopped)
        {
            //InitServer runs before Run, so wait for the main loop instead of treating "not started yet" as a dead server
            //A shutdown during the wait exits silently, the same path as a normal stop
            if (!_server.Running)
            {
                if (_server.Stopping) break;
                Thread.Sleep(50);
                continue;
            }
            var nextTickTime = _server.NextTickTimeMillis;
            var currentTime = _server.TickClockMillis;
            //Run sets the running flag before it initializes the target time; a zero target is not a stall
            if (nextTickTime == 0)
            {
                Thread.Sleep(50);
                continue;
            }
            var behind = currentTime - nextTickTime;
            if (behind > _maxTickTimeMillis)
            {
                ReportCrash(behind);
                return;
            }
            //Sleep until the deadline rather than a fixed beat, so a short stall is caught at the threshold and a healthy loop costs one wakeup per threshold
            var sleep = Math.Clamp(_maxTickTimeMillis - behind, 50, 10_000);
            Thread.Sleep((int)sleep);
        }
        Log.Info("Server watchdog stopped");
    }

    private void ReportCrash(long behindMillis)
    {
        //Double check after the decision; a shutdown in progress must not be reported as a crash
        if (_stopped || !_server.Running) return;
        var tookSeconds = (behindMillis / 1000.0).ToString("0.00", CultureInfo.InvariantCulture);
        var expectedSeconds = (_server.TickRate.MillisecondsPerTick / 1000.0).ToString("0.00", CultureInfo.InvariantCulture);
        Log.Error("A single server tick took " + tookSeconds + " seconds (should be max " + expectedSeconds + ")");
        Log.Error("Considering it to be crashed, server will forcibly shutdown.");
        var report = CreateWatchdogCrashReport("Watching Server", behindMillis);
        AddReportDetails(report, behindMillis);
        Log.Critical("Crash report:\n" + report.GetFriendlyReport(ReportType.Crash));
        //The report goes through the shared crash handler so it lands in the program-root crash-reports with the rest
        //The native error window is disabled: the process is about to be halted and nobody may close a modal in time
        var path = CrashHandler.Save(report, null, ReportType.Crash, showWindow: false);
        if (path is not null)
            Log.Error("This crash report has been saved to: " + Path.GetFullPath(path));
        else
            Log.Error("We were unable to save this crash report to disk.");
        Exit();
    }

    //CreateWatchdogCrashReport builds the report with the target describing the stall, maps to vanilla createWatchdogCrashReport
    public static CrashReport CreateWatchdogCrashReport(string message, long behindMillis)
    {
        var error = new Exception("Watchdog (" + message + ")");
        var report = new CrashReport(message, error);
        var dump = new StringBuilder();
        //OS thread table; the managed thread name is unavailable here, the state and wait reason still tell a blocked main thread
        //from a process that lost its CPU slice. StartTime and WaitReason are only available for some threads, so each read is guarded
        using (var process = Process.GetCurrentProcess())
        {
            foreach (ProcessThread thread in process.Threads)
            {
                dump.Append(thread.Id).Append(" priority=").Append(thread.CurrentPriority);
                try { dump.Append(" state=").Append(thread.ThreadState); }
                catch (Exception) { dump.Append(" state=unknown"); }
                try { dump.Append(" wait=").Append(thread.WaitReason); }
                catch (Exception) { dump.Append(" wait=unknown"); }
                try { dump.Append(" started=").Append(thread.StartTime.ToString("O", CultureInfo.InvariantCulture)); }
                catch (Exception) { dump.Append(" started=unknown"); }
                dump.Append(Environment.NewLine);
            }
        }
        //The watchdog thread's own stack, so the dump at least has one managed stack
        dump.Append("Watchdog stack:").Append(Environment.NewLine).Append(Environment.StackTrace);
        report.AddCategory("Thread Dump").SetDetail("Threads", dump.ToString());
        return report;
    }

    private void AddReportDetails(CrashReport report, long behindMillis)
    {
        var statistics = report.AddCategory("Performance stats");
        statistics.SetDetail("Tick behind (ms)", behindMillis);
        statistics.SetDetail("Ticks run", _server.TickCount);
        statistics.SetDetail("Max tick length (ms)", _maxTickTimeMillis);
    }

    //Exit asks the runtime to shut down and halts the process if it does not within the grace period, maps to vanilla exit
    private static void Exit()
    {
        try
        {
            //The timer is rooted by the runtime timer queue while it is scheduled, so no local reference is needed
            _ = new Timer(_ => Environment.FailFast("Watchdog forced shutdown"), null,
                MaxShutdownTimeMillis, Timeout.Infinite);
            Environment.Exit(ShutdownStatus);
        }
        catch (Exception e)
        {
            Log.Error($"Watchdog graceful shutdown failed, halting: {e.Message}");
            Environment.FailFast("Watchdog shutdown", e);
        }
    }
}
