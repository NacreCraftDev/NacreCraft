namespace NacreCraft.Game.Server.Notifications;

//ServerActivityMonitor rate-limits the activity notification, maps to vanilla net.minecraft.server.notifications.ServerActivityMonitor
public sealed class ServerActivityMonitor
{
    private readonly long _minimumMillisBetweenNotifications;
    private long _lastNotificationTime;
    private int _serverActivity;
    private readonly NotificationManager _notificationManager;

    public ServerActivityMonitor(NotificationManager notificationManager, int secondsBetweenNotifications)
    {
        _notificationManager = notificationManager;
        _minimumMillisBetweenNotifications = secondsBetweenNotifications * 1000L;
    }

    public void Tick() => ProcessWithRateLimit();

    public void ReportLoginActivity()
    {
        Interlocked.Exchange(ref _serverActivity, 1);
        ProcessWithRateLimit();
    }

    private void ProcessWithRateLimit()
    {
        var now = Environment.TickCount64;
        if (Volatile.Read(ref _serverActivity) == 1 && now - Volatile.Read(ref _lastNotificationTime) >= _minimumMillisBetweenNotifications)
        {
            _notificationManager.ServerActivityOccured();
            Interlocked.Exchange(ref _lastNotificationTime, Environment.TickCount64);
        }
        Interlocked.Exchange(ref _serverActivity, 0);
    }
}
