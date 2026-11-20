using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Game.Server;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Portal;

//TeleportTransition a pending level transition, maps to vanilla net.minecraft.world.level.portal.TeleportTransition
//Carries the destination level and position, velocity, facing, the missing-respawn-block flag, passenger mode, relative flags
//and a post-transition action. The vanilla record is a plain class here because the post action is a delegate
public sealed class TeleportTransition
{
    public TeleportTransition(ServerLevel newLevel, Vec3 position, Vec3 deltaMovement, float yRot, float xRot,
        PostTeleportTransition postTeleportTransition)
        : this(newLevel, position, deltaMovement, yRot, xRot, false, false, new HashSet<RelativeFlag>(),
            postTeleportTransition)
    {
    }

    public TeleportTransition(ServerLevel newLevel, Vec3 position, Vec3 deltaMovement, float yRot, float xRot,
        IReadOnlySet<RelativeFlag> relatives, PostTeleportTransition postTeleportTransition)
        : this(newLevel, position, deltaMovement, yRot, xRot, false, false, relatives, postTeleportTransition)
    {
    }

    public TeleportTransition(ServerLevel newLevel, Vec3 position, Vec3 deltaMovement, float yRot, float xRot,
        bool missingRespawnBlock, bool asPassenger, IReadOnlySet<RelativeFlag> relatives,
        PostTeleportTransition postTeleportTransition)
    {
        NewLevel = newLevel;
        Position = position;
        DeltaMovement = deltaMovement;
        YRot = yRot;
        XRot = xRot;
        MissingRespawnBlock = missingRespawnBlock;
        AsPassenger = asPassenger;
        Relatives = relatives;
        PostTeleportTransition = postTeleportTransition;
    }

    public ServerLevel NewLevel { get; }
    public Vec3 Position { get; }
    public Vec3 DeltaMovement { get; }
    public float YRot { get; }
    public float XRot { get; }
    public bool MissingRespawnBlock { get; }
    public bool AsPassenger { get; }
    public IReadOnlySet<RelativeFlag> Relatives { get; }
    public PostTeleportTransition PostTeleportTransition { get; }

    //WithRotation copies the transition with a different facing, maps to vanilla withRotation
    public TeleportTransition WithRotation(float yRot, float xRot)
        => new(NewLevel, Position, DeltaMovement, yRot, xRot, MissingRespawnBlock, AsPassenger, Relatives,
            PostTeleportTransition);

    //WithPosition copies the transition with a different destination, maps to vanilla withPosition
    public TeleportTransition WithPosition(Vec3 position)
        => new(NewLevel, position, DeltaMovement, YRot, XRot, MissingRespawnBlock, AsPassenger, Relatives,
            PostTeleportTransition);

    //TransitionAsPassenger marks the transition as riding another entity, maps to vanilla transitionAsPassenger
    public TeleportTransition TransitionAsPassenger()
        => new(NewLevel, Position, DeltaMovement, YRot, XRot, MissingRespawnBlock, true, Relatives,
            PostTeleportTransition);

    //CreateDefault the transition back to the world spawn, maps to vanilla createDefault
    //NC keeps the respawn dimension in the server overworld level and the spawn in the level data; the spawn adjustment
    //of the vanilla path needs the block-below spawn rule, which the world spawn already went through
    public static TeleportTransition CreateDefault(ServerPlayer player, PostTeleportTransition postTeleportTransition)
        => CreateRespawn(player, postTeleportTransition, missingRespawnBlock: false);

    //MissingRespawnBlock the transition used when the respawn block is gone, maps to vanilla missingRespawnBlock
    //Named ForMissingRespawnBlock because the instance flag already owns the MissingRespawnBlock name
    public static TeleportTransition ForMissingRespawnBlock(ServerPlayer player,
        PostTeleportTransition postTeleportTransition)
        => CreateRespawn(player, postTeleportTransition, missingRespawnBlock: true);

    private static TeleportTransition CreateRespawn(ServerPlayer player,
        PostTeleportTransition postTeleportTransition, bool missingRespawnBlock)
    {
        var server = player.Server;
        var newLevel = (ServerLevel)(server?.GetLevel(LevelKeys.OVERWORLD) ?? player.Level);
        var spawn = server?.SpawnPos ?? Vec3.Zero;
        var spawnPos = new BlockPos((int)Math.Floor(spawn.X), (int)Math.Floor(spawn.Y), (int)Math.Floor(spawn.Z));
        var yaw = server?.LevelData.SpawnYaw ?? 0f;
        var pitch = server?.LevelData.SpawnPitch ?? 0f;
        return new TeleportTransition(newLevel, Vec3.AtBottomCenterOf(spawnPos), Vec3.Zero, yaw, pitch,
            missingRespawnBlock, false, new HashSet<RelativeFlag>(), postTeleportTransition);
    }
}

//PostTeleportTransition a callback run after the entity arrives, maps to vanilla TeleportTransition.PostTeleportTransition
public delegate void PostTeleportTransition(NacreCraft.Registry.Entity entity);

//PostTeleportTransitions the built-in post-transition actions, maps to the vanilla DO_NOTHING/PLAY_PORTAL_SOUND/PLACE_PORTAL_TICKET
public static class PostTeleportTransitions
{
    //DoNothing the empty action
    public static readonly PostTeleportTransition DoNothing = _ => { };

    //PlayPortalSound plays the portal travel sound for a server player, maps to vanilla playPortalSound
    public static readonly PostTeleportTransition PlayPortalSound = entity =>
    {
        if (entity is ServerPlayer player)
            player.Connection.Send(new ClientboundLevelEventPacket(1032, BlockPos.Zero, 0, false));
    };

    //PlacePortalTicket would keep the destination chunk loaded, maps to vanilla placePortalTicket
    //The ticket API is not exposed on the entity yet, so this is a no-op until the entity ticket hooks land
    public static readonly PostTeleportTransition PlacePortalTicket = _ => { };

    //Then chains two actions, maps to the vanilla default then
    public static PostTeleportTransition Then(this PostTeleportTransition first, PostTeleportTransition second)
        => entity =>
        {
            first(entity);
            second(entity);
        };
}
