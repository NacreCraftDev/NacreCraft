using NacreCraft.Commands;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Logging;
using NacreCraft.Network.Chat;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.Commands;

//ServerCommandSource server command source, maps to the server part of vanilla CommandSourceStack
//Two kinds: a player-initiated one holding the executing player and replying via the system chat packet; a console-initiated one with no player and full permission, replying via the log
//The with* methods (/execute) layer entity/position/rotation/anchor/level overrides on top without losing the feedback channel
public sealed class ServerCommandSource : CommandSourceStack
{
    //_position falls back to this when there is no entity; command blocks execute at their own block position
    private readonly Vec3 _position;

    //_level falls back to this when there is no entity; command blocks keep the level they live in
    private readonly ServerLevel? _level;

    //_entity the entity the command operates on (set by /execute as); null means the executing player
    private readonly NacreCraft.Registry.Entity? _entity;

    //_positionOverride position written by /execute positioned|align|anchored
    private readonly Vec3? _positionOverride;

    //_rotationOverride rotation written by /execute rotated|facing
    private readonly Vec2? _rotationOverride;

    //_anchor which reference point the source resolves positions from
    private readonly EntityAnchorArgument.Anchor _anchor;

    //_levelOverride level written by /execute in|at
    private readonly ServerLevel? _levelOverride;

    //_silent suppresses feedback, maps to vanilla withSuppressedOutput
    private readonly bool _silent;

    //Player-initiated; the permission level is the player's current level 0-4 from the ops.json list
    public ServerCommandSource(ServerPlayer player, MinecraftServer server)
        : base(player.Profile.Name, player.PermissionLevel, TextWriter.Null)
    {
        Player = player;
        Server = server;
        _position = player.Position;
        _level = player.Level;
        _anchor = EntityAnchorArgument.Anchor.Feet;
    }

    //Console-initiated; name and permission are given by the caller
    private ServerCommandSource(string name, int permissionLevel, MinecraftServer server, TextWriter? output = null,
        bool acceptsSuccess = true, bool acceptsFailure = true, Vec3 position = default, ServerLevel? level = null)
        : base(name, permissionLevel, output ?? TextWriter.Null, acceptsSuccess, acceptsFailure)
    {
        Player = null;
        Server = server;
        _position = position;
        _level = level;
        _anchor = EntityAnchorArgument.Anchor.Feet;
    }

    //Copy constructor used by WithCallback; every field that decides where a command runs and who it runs as is carried over
    private ServerCommandSource(ServerCommandSource other, CommandResultCallback callback)
        : base(other.SenderName, other.PermissionLevel, other.Output, other.AcceptsSuccess, other.AcceptsFailure)
    {
        Player = other.Player;
        Server = other.Server;
        _position = other._position;
        _level = other._level;
        _entity = other._entity;
        _positionOverride = other._positionOverride;
        _rotationOverride = other._rotationOverride;
        _anchor = other._anchor;
        _levelOverride = other._levelOverride;
        _silent = other._silent;
        Callback = callback;
    }

    //Copy constructor used by the with* methods; null arguments keep the current value
    private ServerCommandSource(ServerCommandSource other, NacreCraft.Registry.Entity? entity, Vec3? positionOverride,
        Vec2? rotationOverride, EntityAnchorArgument.Anchor? anchor, ServerLevel? levelOverride, bool? silent)
        : base(other.SenderName, other.PermissionLevel, other.Output,
            other.AcceptsSuccess && silent != true, other.AcceptsFailure && silent != true)
    {
        Player = other.Player;
        Server = other.Server;
        _position = other._position;
        _level = other._level;
        _entity = entity ?? other._entity;
        _positionOverride = positionOverride ?? other._positionOverride;
        _rotationOverride = rotationOverride ?? other._rotationOverride;
        _anchor = anchor ?? other._anchor;
        _levelOverride = levelOverride ?? other._levelOverride;
        _silent = silent ?? other._silent;
        Callback = other.Callback;
    }

    //WithCallback returns a server source again, so command executors can still cast to ServerCommandSource
    public override ServerCommandSource WithCallback(CommandResultCallback callback) => new(this, callback);

    //Console the console command source, for cases with no executing entity such as the server GUI input box
    //maps to vanilla DedicatedServer.createCommandSourceStack with sender name Server and full permission
    public static ServerCommandSource Console(MinecraftServer server) => new("Server", 4, server);

    //GameLoop the game loop command source, maps to vanilla ServerFunctionManager.getGameLoopSender
    //Name Server, permission gamemasters, output suppressed
    public static ServerCommandSource GameLoop(MinecraftServer server)
        => new("Server", (int)NacreCraft.Registry.PermissionLevel.Gamemasters, server, null, false, false);

    //Rcon the RCON command source, replying into the caller-provided buffer, maps to vanilla RconConsoleSource.createCommandSourceStack
    //Name Rcon, permission LevelBasedPermissionSet.OWNER i.e. level 4
    //PermissionLevel is shadowed by the base class PermissionLevel property here, so it must be fully qualified
    public static ServerCommandSource Rcon(MinecraftServer server, TextWriter output)
        => new("Rcon", (int)NacreCraft.Registry.PermissionLevel.Owners, server, output);

    //CommandBlock the command block command source, maps to vanilla CommandBlockEntity.createCommandSourceStack
    //Executes at the block position with game master permission and captures output into the caller's writer
    public static ServerCommandSource CommandBlock(MinecraftServer server, ServerLevel level, Vec3 position, TextWriter output)
        => new("CommandBlock", (int)NacreCraft.Registry.PermissionLevel.Gamemasters, server, output,
            position: position, level: level);

    //Player the command executor, null when console-initiated
    public ServerPlayer? Player { get; }

    //The command needs a player but the source has none, maps to vanilla CommandSourceStack.ERROR_NOT_PLAYER
    public static readonly SimpleCommandExceptionType ErrorRequiresPlayer =
        new(new TranslatableMessage("permissions.requires.player"));

    //Entity the entity the command operates on: the /execute as target when one was set, otherwise the executing player
    //Feedback still goes through Player, so a redirected source keeps replying on the original connection
    public NacreCraft.Registry.Entity? Entity => _entity ?? Player;

    //PlayerOrThrow gets the operating entity as a player; a non-player source throws a syntax error
    //maps to vanilla CommandSourceStack.getPlayerOrException; this is what @s resolves through
    public ServerPlayer PlayerOrThrow => Entity as ServerPlayer ?? throw ErrorRequiresPlayer.Create();

    //Server server reference, for selectors to get online players and broadcast
    //The type is the base class MinecraftServer, aligned with vanilla CommandSourceStack.getServer
    public MinecraftServer Server { get; }

    //Position the command source coordinate, maps to vanilla CommandSourceStack.getPosition
    //A /execute positioned/align/anchored value wins, then the operating entity, then the fallback position
    public Vec3 Position => _positionOverride ?? Entity?.Pos ?? _position;

    //Rotation the command source facing, maps to vanilla CommandSourceStack.getRotation
    //A /execute rotated/facing value wins, then the operating entity; any source without an entity reports zero rotation
    public (float Yaw, float Pitch) Rotation => _rotationOverride is { } overridden
        ? (overridden.Y, overridden.X)
        : Entity is { } entity ? (entity.YRot, entity.XRot) : (0f, 0f);

    //Level the level this source executes in; command blocks pass their own level, /execute in overrides it
    public ServerLevel? Level => _levelOverride ?? (Entity?.Level as ServerLevel) ?? _level;

    //Anchor the reference point positions resolve from, maps to vanilla CommandSourceStack.getAnchor
    public EntityAnchorArgument.Anchor Anchor => _anchor;

    //AnchorPosition the world coordinate of the anchor point, maps to vanilla Anchor.apply(CommandSourceStack)
    public Vec3 AnchorPosition => _anchor == EntityAnchorArgument.Anchor.Eyes && Entity is { } entity
        ? entity.GetEyePosition()
        : Position;

    //--- with* overrides used by /execute, maps to the vanilla CommandSourceStack with* family ---

    //WithEntity switches the operating entity, maps to vanilla withEntity
    public ServerCommandSource WithEntity(NacreCraft.Registry.Entity entity)
        => new(this, entity, null, null, null, null, null);

    //WithPosition overrides the source position, maps to vanilla withPosition
    public ServerCommandSource WithPosition(Vec3 position)
        => new(this, null, position, null, null, null, null);

    //WithRotation overrides the source rotation, maps to vanilla withRotation(Vec2)
    public ServerCommandSource WithRotation(Vec2 rotation)
        => new(this, null, null, rotation, null, null, null);

    //WithRotation yaw/pitch convenience, maps to calling withRotation(new Vec2(pitch, yaw))
    public ServerCommandSource WithRotation(float yaw, float pitch)
        => WithRotation(new Vec2(pitch, yaw));

    //WithAnchor switches the reference point, maps to vanilla withAnchor
    public ServerCommandSource WithAnchor(EntityAnchorArgument.Anchor anchor)
        => new(this, null, null, null, anchor, null, null);

    //WithLevel switches the executing level, maps to vanilla withLevel
    public ServerCommandSource WithLevel(ServerLevel level)
        => new(this, null, null, null, null, level, null);

    //WithSuppressedOutput hides success and failure feedback, maps to vanilla withSuppressedOutput
    public ServerCommandSource WithSuppressedOutput()
        => new(this, null, null, null, null, null, true);

    //Facing turns the source toward a world coordinate, maps to vanilla CommandSourceStack.facing(Vec3)
    public ServerCommandSource Facing(Vec3 target)
    {
        var from = AnchorPosition;
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;
        var dz = target.Z - from.Z;
        var horizontal = Math.Sqrt(dx * dx + dz * dz);
        var pitch = Mth.WrapDegrees((float)-(Math.Atan2(dy, horizontal) * (180.0 / Math.PI)));
        var yaw = Mth.WrapDegrees((float)(Math.Atan2(dz, dx) * (180.0 / Math.PI)) - 90.0f);
        return WithRotation(yaw, pitch);
    }

    //Facing turns the source toward an entity anchor, maps to vanilla facing(Entity, Anchor)
    public ServerCommandSource Facing(NacreCraft.Registry.Entity entity, EntityAnchorArgument.Anchor anchor)
        => Facing(EntityAnchorArgument.Apply(anchor, entity));

    public override void SendSuccess(string message)
        => Send(message, AcceptsSuccess);

    public override void SendFailure(string message)
        => Send(message, AcceptsFailure);

    //Send a single reply; a player-initiated one sends the system chat packet with overlay false through the chat bar
    //One with an output buffer (such as RCON) writes back to the buffer, maps to vanilla RconConsoleSource's buffer behavior
    //None of those falls back to the log, maps to vanilla printing to the server console
    private void Send(string message, bool accepted)
    {
        if (!accepted) return;
        if (Player is { } player)
            player.Connection.Send(new ClientboundSystemChatPacket(Component.Literal(message), false));
        else if (!ReferenceEquals(Output, TextWriter.Null))
            Output.WriteLine(message);
        else
            Log.Info($"[Server] {message}");
    }
}
