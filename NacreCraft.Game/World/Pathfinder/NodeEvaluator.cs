using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Level;
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Phys.Collision;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Pathfinder;

//NodeEvaluator base of the four node evaluators, maps to vanilla net.minecraft.world.level.pathfinder.NodeEvaluator
//Owns the per-run pathfinding context and the node table shared by one evaluation
public abstract class NodeEvaluator
{
    //Nodes deduplicated by the packed position hash so one cell always maps to one node, maps to vanilla nodes map
    protected Dictionary<int, Node> Nodes { get; } = new();

    protected PathfindingContext CurrentContext { get; set; } = null!;

    protected Mob Mob { get; set; } = null!;

    protected int EntityWidth { get; set; }

    protected int EntityHeight { get; set; }

    protected int EntityDepth { get; set; }

    protected bool _canOpenDoors;

    protected bool _canFloat;

    protected bool _canWalkOverFences;

    protected bool _canPassDoors = true;

    public abstract Node? GetStart();

    public abstract Target GetTarget(double x, double y, double z);

    public abstract int GetNeighbors(Node[] neighbors, Node pos);

    public abstract PathType GetPathTypeOfMob(PathfindingContext context, int x, int y, int z, Mob mob);

    public abstract PathType GetPathType(PathfindingContext context, int x, int y, int z);

    //Prepare resets the per-run state and sizes the entity box in cells, maps to vanilla prepare
    public virtual void Prepare(PathNavigationRegion level, Mob entity)
    {
        CurrentContext = new PathfindingContext(level, entity);
        Mob = entity;
        Nodes.Clear();
        EntityWidth = Mth.Floor((float)entity.Width + 1.0f);
        EntityHeight = Mth.Floor((float)entity.Height + 1.0f);
        EntityDepth = Mth.Floor((float)entity.Width + 1.0f);
    }

    public virtual void Done()
    {
        CurrentContext = null!;
        Mob = null!;
    }

    protected Node GetNode(BlockPos pos) => GetNode(pos.X, pos.Y, pos.Z);

    protected Node GetNode(int x, int y, int z)
    {
        var hash = Node.CreateHash(x, y, z);
        if (Nodes.TryGetValue(hash, out var node)) return node;
        node = new Node(x, y, z);
        Nodes[hash] = node;
        return node;
    }

    protected Target GetTargetNodeAt(double x, double y, double z)
        => new(GetNode(Mth.Floor(x), Mth.Floor(y), Mth.Floor(z)));

    //GetPathType path type at a position seen from the mob, maps to vanilla getPathType(Mob, BlockPos)
    //The entity level is typed object in this project; a level that cannot answer collision queries answers BLOCKED
    public PathType GetPathType(Mob mob, BlockPos pos)
        => mob.Level is CollisionGetter level
            ? GetPathType(new PathfindingContext(level, mob), pos.X, pos.Y, pos.Z)
            : PathType.BLOCKED;

    public void SetCanPassDoors(bool canPassDoors) => _canPassDoors = canPassDoors;

    public void SetCanOpenDoors(bool canOpenDoors) => _canOpenDoors = canOpenDoors;

    public void SetCanFloat(bool canFloat) => _canFloat = canFloat;

    public void SetCanWalkOverFences(bool canWalkOverFences) => _canWalkOverFences = canWalkOverFences;

    public bool CanPassDoors() => _canPassDoors;

    public bool CanOpenDoors() => _canOpenDoors;

    public bool CanFloat() => _canFloat;

    public bool CanWalkOverFences() => _canWalkOverFences;

    //IsBurningBlock blocks that set a walker on fire, maps to vanilla isBurningBlock
    //Magma block and lava cauldron are not registered yet, they are matched by registry name
    public static bool IsBurningBlock(BlockState blockState)
        => blockState.Is(BlockTags.Fire) || blockState.Is(Blocks.LAVA)
           || PathfinderBlocks.IsNamed(blockState, "magma_block")
           || Blocks.CampfireBlock.IsLitCampfire(blockState)
           || PathfinderBlocks.IsNamed(blockState, "lava_cauldron");
}
