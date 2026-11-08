using NacreCraft.Game.Server.JsonRpc.InternalApi;
using NacreCraft.Game.Server.JsonRpc.Security;
using NacreCraft.Game.Server.Notifications;
using NacreCraft.Logging;

namespace NacreCraft.Game.Server.JsonRpc;

//JsonRpc management server bootstrap, maps to vanilla net.minecraft.server.jsonrpc.JsonRpc
//Reads the management-server-* settings and, when enabled, starts the listener and registers the notification bridge
public static class JsonRpc
{
    //Create starts the management server when enabled, maps to vanilla JsonRpc.create
    //TLS is refused because the HttpListener transport cannot serve it without a machine certificate binding
    public static ManagementServer? Create(ServerSettings settings, NotificationManager notificationManager)
    {
        if (!settings.ManagementServerEnabled) return null;
        var secret = settings.ManagementServerSecret;
        if (!SecurityConfig.IsValid(secret))
        {
            throw new InvalidOperationException(
                "Invalid management server secret, must be 40 alphanumeric characters");
        }
        var authenticator = new JsonRpcAuthenticator(new SecurityConfig(secret),
            settings.ManagementServerAllowedOrigins);
        Log.Info($"Starting json RPC server on {settings.ManagementServerHost}:{settings.ManagementServerPort}");
        var server = new ManagementServer(settings.ManagementServerHost, settings.ManagementServerPort, authenticator);
        var minecraftApi = MinecraftApi.Of(notificationManager);
        notificationManager.RegisterService(new JsonRpcNotificationService(minecraftApi, server));
        if (settings.ManagementServerTlsEnabled)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate;
            try
            {
                certificate = JsonRpcSslContextProvider.CreateFrom(settings.ManagementServerTlsKeystore,
                    settings.ManagementServerTlsKeystorePassword);
            }
            catch (Exception e)
            {
                JsonRpcSslContextProvider.PrintInstructions();
                throw new InvalidOperationException(
                    "Failed to configure TLS for the server management protocol", e);
            }
            server.StartWithTls(minecraftApi, certificate);
        }
        else
        {
            server.StartWithoutTls(minecraftApi);
        }
        server.ScheduleHeartbeat(notificationManager, settings.StatusHeartbeatInterval);
        return server;
    }
}
