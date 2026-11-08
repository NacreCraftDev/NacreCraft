using NacreCraft.Game.Server.Notifications;

namespace NacreCraft.Game.Server.JsonRpc.InternalApi;

//MinecraftApi server services facade used by RPC methods, maps to vanilla MinecraftApi
//The allow-list/player-list/gamerule/ban-list/operator services are wired; the settings/state/executor services land with the
//remaining internalapi slice. Submit is the main-thread hop, degraded to the thread pool until the server executor is exposed
public class MinecraftApi
{
    public MinecraftApi(NotificationManager notificationManager, MinecraftAllowListService allowListService,
        MinecraftPlayerListService playerListService, MinecraftGameRuleService gameRuleService,
        MinecraftBanListService? banListService = null, MinecraftOperatorListService? operatorListService = null,
        MinecraftServerSettingsService? serverSettingsService = null,
        MinecraftServerStateService? serverStateService = null,
        MinecraftExecutorService? executorService = null)
    {
        NotificationManager = notificationManager;
        AllowListService = allowListService;
        PlayerListService = playerListService;
        GameRuleService = gameRuleService;
        var logger = new JsonRpcLogger();
        BanListService = banListService ?? new MinecraftBanListServiceImpl(notificationManager, logger);
        OperatorListService = operatorListService ?? new MinecraftOperatorListServiceImpl(notificationManager, logger);
        ServerSettingsService = serverSettingsService ?? new MinecraftServerSettingsServiceImpl(notificationManager, logger);
        ServerStateService = serverStateService ?? new MinecraftServerStateServiceImpl(notificationManager, logger);
        ExecutorService = executorService ?? new MinecraftExecutorServiceImpl(notificationManager);
    }

    //NotificationManager the notification bus the RPC notification service subscribes to
    public NotificationManager NotificationManager { get; }

    //AllowListService whitelist operations
    public MinecraftAllowListService AllowListService { get; }

    //PlayerListService online player and profile lookups
    public MinecraftPlayerListService PlayerListService { get; }

    //GameRuleService gamerule reads and writes
    public MinecraftGameRuleService GameRuleService { get; }

    //BanListService player and IP ban operations
    public MinecraftBanListService BanListService { get; }

    //OperatorListService operator list operations
    public MinecraftOperatorListService OperatorListService { get; }

    //ServerSettingsService server.properties settings behind the management RPC
    public MinecraftServerSettingsService ServerSettingsService { get; }

    //ServerStateService save/stop/system-message lifecycle operations
    public MinecraftServerStateService ServerStateService { get; }

    //ExecutorService the server-thread task hop
    public MinecraftExecutorService ExecutorService { get; }

    //Submit runs work on the server thread, maps to vanilla MinecraftApi.submit
    public Task<T> Submit<T>(Func<T> supplier) => ExecutorService.Submit(supplier);

    //Submit runs an action on the server thread
    public Task Submit(System.Action action) => ExecutorService.Submit(action);

    public static MinecraftApi Of(NotificationManager notificationManager)
    {
        var logger = new JsonRpcLogger();
        return new MinecraftApi(notificationManager,
            new MinecraftAllowListServiceImpl(notificationManager, logger),
            new MinecraftPlayerListServiceImpl(notificationManager, logger),
            new MinecraftGameRuleServiceImpl(notificationManager, logger),
            new MinecraftBanListServiceImpl(notificationManager, logger),
            new MinecraftOperatorListServiceImpl(notificationManager, logger));
    }
}
