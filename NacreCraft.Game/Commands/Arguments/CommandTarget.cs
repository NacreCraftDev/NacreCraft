using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Commands.Arguments;

//CommandTarget command-layer entity target, wrapping both players and level entities
//Selector filtering and sorting happen in this layer; execution dispatches concrete behavior by source
//A player is an entity now, so one reference covers both and only the player-only views stay derived
public sealed class CommandTarget
{
    private CommandTarget(Entity source) => Source = source;

    //Source the wrapped target; a player arrives here as itself rather than through a parallel field
    public Entity Source { get; }

    //Player the player behind the target; null for a level entity
    //This is the one thing the entity view cannot answer, so it is the only branch left
    public ServerPlayer? Player => Source as ServerPlayer;

    //WorldEntity the level entity behind the target; null for a player
    //Kept because a handful of call sites need the narrow type back
    public Entity? WorldEntity => Source as ServerPlayer is null ? Source : null;

    //OfPlayer wraps a player target
    public static CommandTarget OfPlayer(ServerPlayer player) => new(player);

    //OfEntity wraps a level entity target
    public static CommandTarget OfEntity(Entity entity) => new(entity);

    //EntityId entity network id; players and entities share the same allocator
    public int EntityId => Source.EntityId;

    //Type entity type
    public EntityType<object>? Type => Source.Type;

    //Uuid unique identifier
    public Guid Uuid => Source.Uuid;

    //Position entity position
    public Vec3 Position => Source.Pos;

    //Yaw yaw angle
    public float Yaw => Source.YRot;

    //Pitch pitch angle
    public float Pitch => Source.XRot;

    //Name entity name; players use the profile name, entities use the registry name
    public string Name => Player?.Profile.Name ?? Source.Id.ToString();

    //GameType game type; null for non-player targets, matching how the vanilla gamemode selector does not match non-players
    public GameType? GameType => Player?.GameType;

    //BoundingBox hit box; players use a fixed size, entities use their own bounding box
    public AABB BoundingBox
        => Player is not null ? EntitySelector.GetBoundingBox(Player) : Source.BoundingBox;
}
