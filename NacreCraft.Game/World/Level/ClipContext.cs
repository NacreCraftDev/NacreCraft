using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Primitives.Phys;
using NacreCraft.Registry.State;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//ClipBlockStateContext ray clip test against a state predicate, maps to vanilla net.minecraft.world.level.ClipBlockStateContext
public sealed class ClipBlockStateContext
{
    public Vec3 From { get; }

    public Vec3 To { get; }

    public Predicate<BlockState> IsTargetBlock { get; }

    public ClipBlockStateContext(Vec3 from, Vec3 to, Predicate<BlockState> block)
    {
        From = from;
        To = to;
        IsTargetBlock = block;
    }
}

//ClipContext ray clip settings, maps to vanilla net.minecraft.world.level.ClipContext
//Fluid shape lookup is not ported to FluidState, so no fluid shape is returned
public sealed class ClipContext
{
    //ShapeGetter resolves the shape a ray should test, maps to vanilla ClipContext.ShapeGetter
    public interface ShapeGetter
    {
        VoxelShape Get(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context);
    }

    //Block which block shape the ray tests, maps to vanilla ClipContext.Block
    //The enum cannot implement ShapeGetter in C#, so GetBlockShape dispatches on it
    public enum Block
    {
        Collider,
        Outline,
        Visual,
        FalldamageResetting,
    }

    //Fluid which fluid states the ray may pick, maps to vanilla ClipContext.Fluid
    public enum Fluid
    {
        None,
        SourceOnly,
        Any,
        Water,
    }

    private readonly Block _block;
    private readonly Fluid _fluid;
    private readonly CollisionContext _collisionContext;

    public Vec3 From { get; }

    public Vec3 To { get; }

    public ClipContext(Vec3 from, Vec3 to, Block block, Fluid fluid, NacreCraft.Registry.Entity entity)
        : this(from, to, block, fluid, CollisionContext.Of(entity))
    {
    }

    public ClipContext(Vec3 from, Vec3 to, Block block, Fluid fluid, CollisionContext collisionContext)
    {
        From = from;
        To = to;
        _block = block;
        _fluid = fluid;
        _collisionContext = collisionContext;
    }

    public VoxelShape GetBlockShape(BlockState state, BlockGetter level, BlockPos pos)
        => _block switch
        {
            Block.Collider => state.GetCollisionShape(level, pos, _collisionContext),
            Block.Outline => state.GetShape(level, pos, _collisionContext),
            Block.Visual => state.GetVisualShape(level, pos, _collisionContext),
            _ => Shapes.Empty(),
        };

    //CanPick whether the given fluid selection accepts this state, maps to vanilla Fluid.canPick
    //The enum itself cannot carry methods in C#, so the predicate lives here
    public static bool CanPick(Fluid fluid, FluidState fluidState) => fluid switch
    {
        Fluid.None => false,
        Fluid.SourceOnly => fluidState.IsSource,
        Fluid.Any => !fluidState.IsEmpty,
        Fluid.Water => fluidState.IsWater,
        _ => false,
    };

    //CanPickFluid whether the configured fluid selection accepts this state
    public bool CanPickFluid(FluidState fluidState) => CanPick(_fluid, fluidState);

    //GetFluidShape answers empty for every fluid because FluidState carries no shape in this project
    //Maps to vanilla getFluidShape, degraded until fluid shapes land
    public VoxelShape GetFluidShape(FluidState fluidState, BlockGetter level, BlockPos pos)
        => Shapes.Empty();
}
