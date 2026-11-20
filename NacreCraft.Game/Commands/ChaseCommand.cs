using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Server.Chase;
using NacreCraft.Logging;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.Commands;

//ChaseCommand chase command, maps to vanilla net.minecraft.server.commands.ChaseCommand
//Chase is a debug tool that mirrors the lead server's first player position to following servers over TCP
//The command keeps one lead server or one follow client per process, and both stop through /chase stop
public static class ChaseCommand
{
    private const string DefaultConnectHost = "localhost";
    private const string DefaultBindAddress = "0.0.0.0";
    private const int DefaultPort = 10000;
    private const int BroadcastIntervalMs = 100;

    private static ChaseServer? _chaseServer;
    private static ChaseClient? _chaseClient;

    //DimensionNameMap short wire names to dimension keys, maps to vanilla DIMENSION_NAMES
    private static readonly IReadOnlyDictionary<string, ResourceKey<Level>> DimensionNameMap =
        new Dictionary<string, ResourceKey<Level>>
        {
            ["o"] = LevelKeys.OVERWORLD,
            ["n"] = LevelKeys.NETHER,
            ["e"] = LevelKeys.END,
        };

    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("chase")
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("follow")
                .Executes(context => Follow(context, DefaultConnectHost, DefaultPort))
                .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                        "host", StringArgumentType.String())
                    .Executes(context => Follow(context,
                        StringArgumentType.GetString(context, "host"), DefaultPort))
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                            "port", IntegerArgumentType.Integer(1, 65535))
                        .Executes(context => Follow(context,
                            StringArgumentType.GetString(context, "host"),
                            IntegerArgumentType.GetInteger(context, "port"))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("lead")
                .Executes(context => Lead(context, DefaultBindAddress, DefaultPort))
                .Then(RequiredArgumentBuilder<CommandSourceStack, string>.Argument(
                        "bind_address", StringArgumentType.String())
                    .Executes(context => Lead(context,
                        StringArgumentType.GetString(context, "bind_address"), DefaultPort))
                    .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument(
                            "port", IntegerArgumentType.Integer(1024, 65535))
                        .Executes(context => Lead(context,
                            StringArgumentType.GetString(context, "bind_address"),
                            IntegerArgumentType.GetInteger(context, "port"))))))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("stop")
                .Executes(Stop)));
    }

    //DimensionName the short wire name for a dimension identifier, null when it is not one of the three vanilla dimensions
    //Maps to the reverse lookup of vanilla DIMENSION_NAMES
    public static string? DimensionName(Identifier dimension)
        => dimension == LevelKeys.OVERWORLD.Identifier ? "o"
            : dimension == LevelKeys.NETHER.Identifier ? "n"
            : dimension == LevelKeys.END.Identifier ? "e"
            : null;

    //DimensionKey resolves a short wire name back to the dimension key, null when unknown
    public static ResourceKey<Level>? DimensionKey(string name)
        => DimensionNameMap.TryGetValue(name, out var key) ? key : null;

    private static int Stop(CommandContext<CommandSourceStack> context)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        if (_chaseClient is not null)
        {
            _chaseClient.Stop();
            source.SendSuccess("You have now stopped chasing");
            _chaseClient = null;
        }
        if (_chaseServer is not null)
        {
            _chaseServer.Stop();
            source.SendSuccess("You are no longer being chased");
            _chaseServer = null;
        }
        return 0;
    }

    private static bool AlreadyRunning(ServerCommandSource source)
    {
        if (_chaseServer is not null)
        {
            source.SendFailure("Chase server is already running. Stop it using /chase stop");
            return true;
        }
        if (_chaseClient is not null)
        {
            source.SendFailure("You are already chasing someone. Stop it using /chase stop");
            return true;
        }
        return false;
    }

    private static int Lead(CommandContext<CommandSourceStack> context, string bindAddress, int port)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        if (AlreadyRunning(source)) return 0;
        _chaseServer = new ChaseServer(bindAddress, port, source.Server.PlayerList, BroadcastIntervalMs);
        try
        {
            _chaseServer.Start();
            source.SendSuccess($"Chase server is now running on port {port}. Clients can follow you using /chase follow <ip> <port>");
            return 0;
        }
        catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException)
        {
            Log.Error($"Failed to start chase server: {e.Message}");
            source.SendFailure($"Failed to start chase server on port {port}");
            _chaseServer = null;
            return 0;
        }
    }

    private static int Follow(CommandContext<CommandSourceStack> context, string host, int port)
    {
        if (context.GetSource() is not ServerCommandSource source) return 0;
        if (AlreadyRunning(source)) return 0;
        _chaseClient = new ChaseClient(host, port, source.Server);
        _chaseClient.Start();
        source.SendSuccess(
            $"You are now chasing {host}:{port}. If that server does '/chase lead' then you will automatically go to the same position. Use '/chase stop' to stop chasing.");
        return 0;
    }
}
