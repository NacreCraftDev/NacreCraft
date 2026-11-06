using NacreCraft.Game.Server.Notifications;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftApi server services facade used by RPC methods, maps to the server access half of vanilla MinecraftApi
//Only the notification manager is wired so far; the allow-list/ban-list/player/gamerule/operator/settings/state services land with
//the internalapi package. Submit is the main-thread hop, degraded to the thread pool until the server executor is exposed
public class MinecraftApi
{
    public MinecraftApi(NotificationManager notificationManager) => NotificationManager = notificationManager;

    //NotificationManager the notification bus the RPC notification service subscribes to
    public NotificationManager NotificationManager { get; }

    //Submit runs work on the server thread, maps to vanilla MinecraftApi.submit
    public virtual Task<T> Submit<T>(Func<T> supplier) => Task.Run(supplier);

    //Submit runs an action on the server thread
    public virtual Task Submit(System.Action action) => Task.Run(action);

    public static MinecraftApi Of(NotificationManager notificationManager) => new(notificationManager);
}
