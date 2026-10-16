using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Level.Block;

//MultifaceBlock a block that sticks to several sides of its own cell, maps to vanilla net.minecraft.world.level.block.MultifaceBlock
//Sculk veins, glow lichen and resin clumps are all multiface blocks: the six sides are independent booleans and a side can
//only hold when the neighbour behind it fills that whole face
//Vanilla also rotates and mirrors these for structures; this port leaves that out, the sculk side never mirrors anyway
//Abstract here because the registry Id is abstract in this port and only a concrete block can name itself
public abstract class MultifaceBlock : BlockBehaviour
{
    //Waterlogged whether the cell is waterlogged, maps to vanilla WATERLOGGED
    public static readonly BooleanProperty Waterlogged = BlockStateProperties.Waterlogged;

    //Faces the six sides, in the order vanilla packs the face bits
    protected static readonly Direction[] Faces = Direction.Values;

    //FacePropertyByDirection the boolean property of each side, maps to vanilla PROPERTY_BY_DIRECTION
    private static readonly Dictionary<Direction, BooleanProperty> FacePropertyByDirection = new()
    {
        [Direction.Down] = BlockStateProperties.FaceDown,
        [Direction.Up] = BlockStateProperties.FaceUp,
        [Direction.North] = BlockStateProperties.FaceNorth,
        [Direction.South] = BlockStateProperties.FaceSouth,
        [Direction.West] = BlockStateProperties.FaceWest,
        [Direction.East] = BlockStateProperties.FaceEast,
    };

    private readonly Dictionary<Direction, VoxelShape> _sideShapes;
    private readonly Dictionary<string, PropertyBase> _stateProperties;

    public MultifaceBlock()
    {
        //A face is a thin plate on the inside of that side, maps to vanilla rotateAll(boxZ(16.0, 0.0, 1.0))
        _sideShapes = Shapes.RotateAll(NacreCraft.Registry.Block.BoxZ(16.0, 0.0, 1.0));
        _stateProperties = new Dictionary<string, PropertyBase>
        {
            ["down"] = BlockStateProperties.FaceDown,
            ["up"] = BlockStateProperties.FaceUp,
            ["north"] = BlockStateProperties.FaceNorth,
            ["south"] = BlockStateProperties.FaceSouth,
            ["west"] = BlockStateProperties.FaceWest,
            ["east"] = BlockStateProperties.FaceEast,
            ["waterlogged"] = BlockStateProperties.Waterlogged,
        };
    }

    //Properties the six sides plus waterlogged, the shape blocks.txt gives every multiface block
    public override IDictionary<string, PropertyBase> Properties => _stateProperties;

    //GetFaceProperty the property of one side, maps to vanilla getFaceProperty
    public static BooleanProperty GetFaceProperty(Direction face) => FacePropertyByDirection[face];

    //HasFace whether the state sticks to that side, maps to vanilla hasFace
    //A side this block does not support simply has no such property, which reads back as false
    public static bool HasFace(BlockState state, Direction face)
        => state.GetValueOrElse(FacePropertyByDirection[face], false);

    //HasAnyFace whether at least one side is occupied, maps to vanilla hasAnyFace
    public static bool HasAnyFace(BlockState state) => Direction.Values.Any(face => HasFace(state, face));

    //Pack the occupied sides into a bit mask, maps to vanilla pack
    //The sculk charge particle event carries it so the client knows which plates to show
    public static byte Pack(IEnumerable<Direction> directions)
    {
        byte code = 0;
        foreach (var direction in directions) code |= (byte)(1 << direction.Id3D);
        return code;
    }

    //Unpack the sides back out of a bit mask, maps to vanilla unpack
    public static IReadOnlyList<Direction> Unpack(byte data)
        => Direction.Values.Where(direction => (data & (byte)(1 << direction.Id3D)) > 0).ToList();

    //AvailableFaces the occupied sides, maps to vanilla availableFaces
    public static IReadOnlyList<Direction> AvailableFaces(BlockState state)
        => state.Owner is MultifaceBlock ? Direction.Values.Where(face => HasFace(state, face)).ToList() : [];

    //CanAttachTo whether the neighbour on that side can carry the face
    //The neighbour's support shape has to fill the whole face facing back, maps to vanilla canAttachTo
    public static bool CanAttachTo(ISculkBlockAccess level, BlockPos pos, Direction towardsNeighbour)
    {
        var neighbourPos = pos.Offset(towardsNeighbour);
        if (level.GetBlockState(neighbourPos) is not { } neighbourState) return false;
        return CanAttachTo(towardsNeighbour, neighbourPos, neighbourState);
    }

    //CanAttachTo from an already known neighbour state
    //A support shape does not depend on what surrounds it, so an empty view of the world stands in, matching vanilla's
    //getBlockSupportShape, which a world generation region cannot answer either
    public static bool CanAttachTo(Direction towardsNeighbour, BlockPos neighbourPos, BlockState neighbourState)
        => neighbourState.Owner is BlockBehaviour behaviour
            && behaviour.IsFaceSturdy(EmptyBlockGetter.Instance, neighbourPos, neighbourState,
                towardsNeighbour.Opposite, SupportType.Full);

    //IsFaceSupported whether this block can stick to that side, maps to vanilla isFaceSupported
    //A subclass with fewer sides overrides it; sculk veins and glow lichen support all six
    protected virtual bool IsFaceSupported(Direction face) => true;

    //IsValidStateForPlacement whether a face may be added to the existing cell from that side
    //maps to vanilla isValidStateForPlacement
    public bool IsValidStateForPlacement(ISculkBlockAccess level, BlockState oldState, BlockPos placementPos,
        Direction placementDirection)
    {
        if (!IsFaceSupported(placementDirection)) return false;
        if (ReferenceEquals(oldState.Owner, this) && HasFace(oldState, placementDirection)) return false;
        return CanAttachTo(level, placementPos, placementDirection);
    }

    //GetStateForPlacement the state with the new face added, null when the placement is refused
    //maps to vanilla getStateForPlacement(BlockState, BlockGetter, BlockPos, Direction)
    public virtual BlockState? GetStateForPlacement(BlockState oldState, ISculkBlockAccess level, BlockPos placementPos,
        Direction placementDirection)
    {
        if (!IsValidStateForPlacement(level, oldState, placementPos, placementDirection)) return null;
        var newState = ReferenceEquals(oldState.Owner, this) ? oldState : DefaultBlockState;
        return newState.SetValue(FacePropertyByDirection[placementDirection], true);
    }

    //GetStateForPlacement from a player click: try the sides in order and take the first that sticks
    //maps to the vanilla BlockPlaceContext overload
    public override BlockState? GetStateForPlacement(ServerLevel level, BlockPos pos, Direction face,
        Direction horizontalFacing)
    {
        if (level.GetBlockState(pos) is not { } oldState) return null;
        var access = new ServerLevelSculkAccess(level);
        foreach (var direction in LookingDirections(face, horizontalFacing))
        {
            var placed = GetStateForPlacement(oldState, access, pos, direction);
            if (placed is not null) return placed;
        }
        return null;
    }

    //LookingDirections the sides a click tries, nearest first
    //Vanilla walks the full nearest-looking order; only the clicked side and its opposite plus the horizontal facing are kept here
    private static IEnumerable<Direction> LookingDirections(Direction face, Direction horizontalFacing)
    {
        yield return face;
        yield return face.Opposite;
        yield return horizontalFacing;
        yield return horizontalFacing.Opposite;
        yield return Direction.Up;
        yield return Direction.Down;
    }

    //UpdateShape drops a face once the neighbour behind it can no longer carry it, maps to vanilla updateShape
    public override BlockState UpdateShape(ServerLevel level, BlockPos pos, BlockState state,
        Direction directionToNeighbour, BlockPos neighbourPos, BlockState neighbourState)
    {
        if (!HasAnyFace(state)) return Blocks.AIR.DefaultBlockState;
        if (!HasFace(state, directionToNeighbour)
            || CanAttachTo(directionToNeighbour, neighbourPos, neighbourState))
            return state;
        return RemoveFace(state, FacePropertyByDirection[directionToNeighbour]);
    }

    //RemoveFace clears one side and turns the cell to air once nothing is left, maps to vanilla removeFace
    private static BlockState RemoveFace(BlockState state, BooleanProperty property)
    {
        var newState = state.SetValue(property, false);
        return HasAnyFace(newState) ? newState : Blocks.AIR.DefaultBlockState;
    }

    //GetShape the union of the plates of every occupied side, maps to vanilla getShape
    public override VoxelShape GetShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context)
    {
        var shape = Shapes.Empty();
        foreach (var face in Direction.Values)
        {
            if (!HasFace(state, face)) continue;
            shape = Shapes.Or(shape, _sideShapes[face]);
        }
        return shape.IsEmpty ? Shapes.Block() : shape;
    }

    //CanSurvive at least one side must still be attached, maps to vanilla canSurvive
    public override bool CanSurvive(ServerLevel level, BlockPos pos, BlockState state)
    {
        var attached = false;
        foreach (var face in Direction.Values)
        {
            if (!HasFace(state, face)) continue;
            if (!CanAttachTo(new ServerLevelSculkAccess(level), pos, face)) return false;
            attached = true;
        }
        return attached;
    }
}
