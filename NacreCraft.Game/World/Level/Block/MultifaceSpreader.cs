using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.Block;

//MultifaceSpreader spreads a multiface block across a surface, maps to vanilla net.minecraft.world.level.block.MultifaceSpreader
//A spread starts from an occupied face and walks toward a direction; the spread kind decides where the new face lands:
//the same cell, the neighbour cell on the same plane, or wrapped around the corner
//Sculk veins use it both while a charge spreads and while a vein grows back onto a sculk block
public sealed class MultifaceSpreader
{
    //DefaultSpreadOrder the order the kinds are tried in, maps to vanilla DEFAULT_SPREAD_ORDER
    public static readonly SpreadKind[] DefaultSpreadOrder =
        [SpreadKind.SamePosition, SpreadKind.SamePlane, SpreadKind.WrapAround];

    private readonly SpreadConfig _config;

    public MultifaceSpreader(MultifaceBlock multifaceBlock) : this(new DefaultSpreaderConfig(multifaceBlock))
    {
    }

    public MultifaceSpreader(SpreadConfig config) => _config = config;

    //CanSpreadInAnyDirection whether any direction has somewhere to go, maps to vanilla canSpreadInAnyDirection
    public bool CanSpreadInAnyDirection(BlockState state, ISculkBlockAccess level, BlockPos pos, Direction startingFace)
        => Direction.Values.Any(direction =>
            GetSpreadFromFaceTowardDirection(state, level, pos, startingFace, direction, _config.CanSpreadInto)
                is not null);

    //SpreadFromRandomFaceTowardRandomDirection tries a random occupied face and a random direction, maps to the vanilla same-named method
    public SpreadPos? SpreadFromRandomFaceTowardRandomDirection(BlockState state, ISculkBlockAccess level, BlockPos pos,
        RandomSource random)
    {
        foreach (var face in Shuffled(random))
        {
            if (!_config.CanSpreadFrom(state, face)) continue;
            var spread = SpreadFromFaceTowardRandomDirection(state, level, pos, face, random, false);
            if (spread is not null) return spread;
        }
        return null;
    }

    //SpreadAll tries every occupied face in every direction and returns how many faces were placed
    //The long return is what SculkBehaviour compares against zero, maps to vanilla spreadAll
    public long SpreadAll(BlockState state, ISculkBlockAccess level, BlockPos pos, bool postProcess)
    {
        long placed = 0;
        foreach (var face in Direction.Values)
        {
            if (!_config.CanSpreadFrom(state, face)) continue;
            placed += SpreadFromFaceTowardAllDirections(state, level, pos, face, postProcess);
        }
        return placed;
    }

    //SpreadFromFaceTowardRandomDirection tries one starting face against the directions in random order
    public SpreadPos? SpreadFromFaceTowardRandomDirection(BlockState state, ISculkBlockAccess level, BlockPos pos,
        Direction startingFace, RandomSource random, bool postProcess)
    {
        foreach (var direction in Shuffled(random))
        {
            var spread = SpreadFromFaceTowardDirection(state, level, pos, startingFace, direction, postProcess);
            if (spread is not null) return spread;
        }
        return null;
    }

    private long SpreadFromFaceTowardAllDirections(BlockState state, ISculkBlockAccess level, BlockPos pos,
        Direction startingFace, bool postProcess)
    {
        long placed = 0;
        foreach (var direction in Direction.Values)
        {
            if (SpreadFromFaceTowardDirection(state, level, pos, startingFace, direction, postProcess) is not null)
                placed++;
        }
        return placed;
    }

    //SpreadFromFaceTowardDirection picks a spot and places a face there, maps to vanilla method of the same name
    public SpreadPos? SpreadFromFaceTowardDirection(BlockState state, ISculkBlockAccess level, BlockPos pos,
        Direction fromFace, Direction spreadDirection, bool postProcess)
    {
        var spreadPos = GetSpreadFromFaceTowardDirection(state, level, pos, fromFace, spreadDirection,
            _config.CanSpreadInto);
        return spreadPos is null ? null : SpreadToFace(level, spreadPos, postProcess);
    }

    //GetSpreadFromFaceTowardDirection decides where a new face would land without placing it
    //The two directions must sit on different axes, otherwise the new face would land back on the plane it came from
    public SpreadPos? GetSpreadFromFaceTowardDirection(BlockState state, ISculkBlockAccess level, BlockPos pos,
        Direction startingFace, Direction spreadDirection, SpreadPredicate canSpreadInto)
    {
        if (spreadDirection.AxisValue == startingFace.AxisValue) return null;
        if (!_config.IsOtherBlockValidAsSource(state)
            && !(_config.HasFace(state, startingFace) && !_config.HasFace(state, spreadDirection)))
            return null;
        foreach (var kind in _config.SpreadTypes)
        {
            var spreadPos = kind.GetSpreadPos(pos, spreadDirection, startingFace);
            if (canSpreadInto(level, pos, spreadPos)) return spreadPos;
        }
        return null;
    }

    //SpreadToFace places the face, maps to vanilla spreadToFace
    public SpreadPos? SpreadToFace(ISculkBlockAccess level, SpreadPos spreadPos, bool postProcess)
    {
        if (level.GetBlockState(spreadPos.Pos) is not { } oldState) return null;
        return _config.PlaceBlock(level, spreadPos, oldState, postProcess) ? spreadPos : null;
    }

    //Shuffled a copy of the six directions in random order, maps to vanilla Direction.allShuffled
    private static List<Direction> Shuffled(RandomSource random)
    {
        var list = new List<Direction>(Direction.Values);
        RandomCollections.Shuffle(list, random);
        return list;
    }
}

//SpreadPos where a new face would be placed and which side it sticks to, maps to vanilla MultifaceSpreader.SpreadPos
public sealed record SpreadPos(BlockPos Pos, Direction Face);

//SpreadPredicate whether a spot can take a new face, maps to vanilla MultifaceSpreader.SpreadPredicate
public delegate bool SpreadPredicate(ISculkBlockAccess level, BlockPos sourcePos, SpreadPos spreadPos);

//SpreadKind where a new face lands relative to the source, maps to vanilla MultifaceSpreader.SpreadType
public enum SpreadKind
{
    //SamePosition the new face lands on the same cell, on the side the spread moved toward
    SamePosition,

    //SamePlane the new face lands on the neighbour cell in the spread direction, keeping the source side
    SamePlane,

    //WrapAround the new face wraps around the corner onto the side of the neighbour cell
    WrapAround
}

//SpreadKindExtensions resolves where each kind puts the face, maps to the vanilla SpreadType.getSpreadPos overrides
public static class SpreadKindExtensions
{
    public static SpreadPos GetSpreadPos(this SpreadKind kind, BlockPos pos, Direction spreadDirection, Direction fromFace)
        => kind switch
        {
            SpreadKind.SamePosition => new SpreadPos(pos, spreadDirection),
            SpreadKind.SamePlane => new SpreadPos(pos.Offset(spreadDirection), fromFace),
            _ => new SpreadPos(pos.Offset(spreadDirection).Offset(fromFace), spreadDirection.Opposite),
        };
}

//SpreadConfig the host-specific half of a spread, maps to vanilla MultifaceSpreader.SpreadConfig
public interface SpreadConfig
{
    //GetStateForPlacement the state to write at the new spot, null when the placement is refused
    BlockState? GetStateForPlacement(BlockState oldState, ISculkBlockAccess level, BlockPos placementPos,
        Direction placementDirection);

    //CanSpreadInto whether the new spot is free and can carry the face
    bool CanSpreadInto(ISculkBlockAccess level, BlockPos sourcePos, SpreadPos spreadPos);

    //SpreadTypes the kinds to try, in order
    SpreadKind[] SpreadTypes => MultifaceSpreader.DefaultSpreadOrder;

    //HasFace whether the state already sticks to that side
    bool HasFace(BlockState state, Direction face) => MultifaceBlock.HasFace(state, face);

    //IsOtherBlockValidAsSource whether a block that is not this one can still start a spread, sculk veins say yes
    bool IsOtherBlockValidAsSource(BlockState state) => false;

    //CanSpreadFrom whether a spread may start from that side
    bool CanSpreadFrom(BlockState state, Direction face) => IsOtherBlockValidAsSource(state) || HasFace(state, face);

    //PlaceBlock writes the new face and returns whether it landed, maps to vanilla placeBlock
    bool PlaceBlock(ISculkBlockAccess level, SpreadPos spreadPos, BlockState oldState, bool postProcess)
    {
        var spreadState = GetStateForPlacement(oldState, level, spreadPos.Pos, spreadPos.Face);
        if (spreadState is null) return false;
        //Vanilla only marks the chunk for post processing during world generation; this project has no such pass
        return level.SetBlock(spreadPos.Pos, spreadState.Value);
    }
}

//DefaultSpreaderConfig the plain multiface spread used by glow lichen and resin, maps to vanilla DefaultSpreaderConfig
public class DefaultSpreaderConfig : SpreadConfig
{
    protected readonly MultifaceBlock Block;

    public DefaultSpreaderConfig(MultifaceBlock block) => Block = block;

    public BlockState? GetStateForPlacement(BlockState oldState, ISculkBlockAccess level, BlockPos placementPos,
        Direction placementDirection)
        => Block.GetStateForPlacement(oldState, level, placementPos, placementDirection);

    //StateCanBeReplaced whether the existing cell may be overwritten: air, the same block, or a water source
    //maps to vanilla stateCanBeReplaced; sculk veins extend it with their own extra rules
    protected virtual bool StateCanBeReplaced(ISculkBlockAccess level, BlockPos sourcePos, BlockPos placementPos,
        Direction placementDirection, BlockState existingState)
        => existingState.Owner.IsAir || ReferenceEquals(existingState.Owner, Block)
            || (ReferenceEquals(existingState.Owner, Blocks.WATER) && existingState.FluidState.IsSource);

    //SpreadTypes the kinds to try, in order; a subclass narrows or reorders them
    public virtual SpreadKind[] SpreadTypes => MultifaceSpreader.DefaultSpreadOrder;

    //IsOtherBlockValidAsSource whether a block that is not this one may still start a spread, maps to vanilla
    public virtual bool IsOtherBlockValidAsSource(BlockState state) => false;

    public bool CanSpreadInto(ISculkBlockAccess level, BlockPos sourcePos, SpreadPos spreadPos)
    {
        if (level.GetBlockState(spreadPos.Pos) is not { } existingState) return false;
        return StateCanBeReplaced(level, sourcePos, spreadPos.Pos, spreadPos.Face, existingState)
            && Block.IsValidStateForPlacement(level, existingState, spreadPos.Pos, spreadPos.Face);
    }
}
