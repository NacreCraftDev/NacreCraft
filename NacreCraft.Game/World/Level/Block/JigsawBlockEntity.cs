using NacreCraft.Game.Bootstrap;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//JigsawBlockEntity jigsaw block block entity, maps to vanilla net.minecraft.world.level.block.entity.JigsawBlockEntity
//Stores the connection name, target, pool and the two priorities; the generate packet assembles a structure from this pool
public sealed class JigsawBlockEntity : BlockEntity
{
    //Default identifiers used when a field is unset, maps to vanilla EMPTY_ID and defaultFinalState
    public static readonly Identifier EmptyId = Identifier.WithDefaultNamespace("empty");

    private const string DefaultFinalState = "minecraft:air";

    public JigsawBlockEntity(BlockPos pos)
        : base(BlockEntityTypes.JIGSAW, pos) { }

    public Identifier Name { get; private set; } = EmptyId;

    public Identifier Target { get; private set; } = EmptyId;

    //Pool the template pool to expand into, minecraft:empty when unset
    public Identifier Pool { get; private set; } = EmptyId;

    public string FinalState { get; private set; } = DefaultFinalState;

    public JointType Joint { get; private set; } = JointType.Rollable;

    public int SelectionPriority { get; private set; }

    public int PlacementPriority { get; private set; }

    public void SetName(Identifier name) => Name = name;

    public void SetTarget(Identifier target) => Target = target;

    public void SetPool(Identifier pool) => Pool = pool;

    public void SetFinalState(string finalState) => FinalState = finalState;

    public void SetJoint(JointType joint) => Joint = joint;

    public void SetSelectionPriority(int value) => SelectionPriority = value;

    public void SetPlacementPriority(int value) => PlacementPriority = value;

    //Generate assembles the pool into the world from this block, maps to vanilla JigsawBlockEntity.generate
    //The start position is one block in front of the jigsaw face; keepJigsaws leaves the connector blocks in place
    public void Generate(ServerLevel level, int levels, bool keepJigsaws)
    {
        if (GameBootstrap.StructureTemplates is not { } manager) return;
        if (level.GetBlockState(Pos) is not { } state) return;
        if (BuiltInRegistries.TEMPLATE_POOL.GetHolder(Pool) is not { } pool) return;
        var front = JigsawBlock.GetFrontFacing(state);
        var position = Pos.Relative(front, 1);
        JigsawPlacement.GenerateInWorld(level, manager, pool, Target, levels, position, keepJigsaws);
    }

    public override void SaveAdditional(CompoundTag tag)
    {
        tag.PutString("name", Name.ToString());
        tag.PutString("target", Target.ToString());
        tag.PutString("pool", Pool.ToString());
        tag.PutString("final_state", FinalState);
        tag.PutString("joint", JointTypes.Name(Joint));
        tag.PutInt("placement_priority", PlacementPriority);
        tag.PutInt("selection_priority", SelectionPriority);
    }

    public override void LoadAdditional(CompoundTag tag)
    {
        Name = ParseIdentifier(tag.GetString("name")?.Value) ?? EmptyId;
        Target = ParseIdentifier(tag.GetString("target")?.Value) ?? EmptyId;
        Pool = ParseIdentifier(tag.GetString("pool")?.Value) ?? EmptyId;
        FinalState = tag.GetString("final_state")?.Value ?? DefaultFinalState;
        var state = Level?.GetBlockState(Pos);
        Joint = (tag.GetString("joint")?.Value is { } jointText ? JointTypes.TryParse(jointText) : null)
            ?? (state is { } blockState ? StructureTemplate.GetDefaultJointType(blockState) : JointType.Rollable);
        PlacementPriority = tag.GetIntOr("placement_priority", 0);
        SelectionPriority = tag.GetIntOr("selection_priority", 0);
    }

    private static Identifier? ParseIdentifier(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : Identifier.TryParse(text);
}
