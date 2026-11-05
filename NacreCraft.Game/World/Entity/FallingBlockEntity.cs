using NacreCraft.Game.World.Level.Block;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.State;

namespace NacreCraft.Game.World.Entity;

//FallingBlockEntity falling sand/gravel style block, maps to vanilla net.minecraft.world.entity.item.FallingBlockEntity
//Falls under gravity and places its block back on landing. Entity damage on impact, waterlogging and block entity data are not ported
public sealed class FallingBlockEntity : NacreCraft.Registry.Entity
{
    public const int DefaultTime = 0;
    public const float DefaultFallDamagePerDistance = 0f;
    public const int DefaultMaxFallDamage = 40;
    public const bool DefaultDropItem = true;
    public const bool DefaultCancelDrop = false;

    //Gravity falling block gravity 0.04, maps to vanilla getDefaultGravity
    public const double Gravity = 0.04;

    private readonly EntityType<object> _type;

    public FallingBlockEntity(EntityType<object> type) => _type = type;

    public FallingBlockEntity(EntityType<object> type, double x, double y, double z, BlockState blockState)
        : this(type)
    {
        Pos = new Vec3(x, y, z);
        BlockState = blockState;
    }

    public override Identifier Id => _type.Id;

    public override EntityType<object>? Type => _type;

    //BlocksBuilding the falling block occupies its cell like a full block, maps to vanilla blocksBuilding
    public override bool BlocksBuilding => true;

    public override double DefaultGravity => Gravity;

    //BlockState the state placed back on landing, maps to vanilla blockState
    public BlockState BlockState { get; set; }

    //Time ticks spent falling, maps to vanilla time
    public int Time { get; set; } = DefaultTime;

    //DropItem whether the block drops as an item when it cannot be placed, maps to vanilla dropItem
    public bool DropItem { get; set; } = DefaultDropItem;

    //CancelDrop suppresses the drop when landing fails, maps to vanilla cancelDrop
    public bool CancelDrop { get; set; } = DefaultCancelDrop;

    //HurtEntities whether landing damages entities, maps to vanilla hurtEntities
    public bool HurtEntities { get; private set; }

    public int FallDamageMax { get; private set; } = DefaultMaxFallDamage;

    public float FallDamagePerDistance { get; private set; } = DefaultFallDamagePerDistance;

    //StartPos the block position the fall started from, maps to vanilla startPos
    public BlockPos StartPos { get; set; }

    //Fall spawns a falling block at a position and clears the source block, maps to vanilla fall
    //EntityTypes has no falling_block entry yet, so the caller supplies the type
    public static FallingBlockEntity Fall(EntityType<object> type, NacreCraft.Storage.ServerLevel level,
        BlockPos pos, BlockState blockState)
    {
        var entity = new FallingBlockEntity(type, pos.X + 0.5, pos.Y, pos.Z + 0.5, blockState)
        {
            StartPos = pos,
        };
        level.SetBlock(pos, Blocks.AIR.DefaultBlockState);
        level.AddEntity(entity);
        return entity;
    }

    public void SetStartPos(BlockPos pos) => StartPos = pos;

    public BlockPos GetStartPos() => StartPos;

    public void SetHurtsEntities(float damagePerDistance, int damageMax)
    {
        HurtEntities = true;
        FallDamagePerDistance = damagePerDistance;
        FallDamageMax = damageMax;
    }

    //Tick falls under gravity and lands, maps to vanilla tick
    public override void Tick()
    {
        TickBase();
        Time++;
        if (Time > 600)
        {
            Discard();
            return;
        }
        Velocity = new Vec3(Velocity.X, Velocity.Y - Gravity, Velocity.Z);
        Move(Velocity);
        var drag = AirDrag;
        Velocity = new Vec3(Velocity.X * drag, Velocity.Y * drag, Velocity.Z * drag);
        if (!OnGround) return;
        Land();
    }

    //Land places the block back, maps to vanilla tick landing branch
    private void Land()
    {
        Discard();
        if (Level is not NacreCraft.Storage.ServerLevel level) return;
        var pos = new BlockPos((int)Math.Floor(Pos.X), (int)Math.Floor(Pos.Y), (int)Math.Floor(Pos.Z));
        level.SetBlock(pos, BlockState);
    }
}
