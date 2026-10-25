using NacreCraft.Nbt;
using NacreCraft.Primitives;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.Block;

//CommandBlockEntity command block block entity, maps to vanilla net.minecraft.world.level.block.entity.CommandBlockEntity
//Stores the command, mode, output and redstone state; the actual command dispatch is injected by the server so the
//Storage layer never needs to know about the command stack
public sealed class CommandBlockEntity : BlockEntity
{
    //Mode command block mode, maps to vanilla CommandBlockEntity.Mode
    public enum Mode
    {
        Impulse,
        Chain,
        Repeating,
    }

    //Executor injected by the server; returns the command result count and the captured output
    //A null executor means commands are not wired yet, the block entity stays inert but keeps its state
    public static Func<ServerLevel, BlockPos, string, (int Success, string Output)>? Executor { get; set; }

    private bool _executionScheduled;

    public CommandBlockEntity(BlockPos pos)
        : this(pos, Mode.Impulse) { }

    public CommandBlockEntity(BlockPos pos, Mode mode)
        : base(BlockEntityTypes.COMMAND_BLOCK, pos)
        => BlockMode = mode;

    //Command the command line; an empty string means the block does nothing
    public string Command { get; private set; } = "";

    //TrackOutput whether the last output is stored and broadcast, maps to vanilla TrackOutput
    public bool TrackOutput { get; private set; } = true;

    //Conditional whether this block only runs when the previous chain block succeeded, maps to vanilla conditional
    public bool Conditional { get; private set; }

    //Automatic whether the block runs once after being configured, maps to vanilla auto
    public bool Automatic { get; private set; }

    //Powered current redstone power state, used to detect the rising edge
    public bool Powered { get; private set; }

    //SuccessCount result of the last execution, maps to vanilla SuccessCount
    public int SuccessCount { get; private set; }

    //LastOutput last captured output, maps to vanilla LastOutput
    public string LastOutput { get; private set; } = "";

    //Mode impulse/chain/repeating, derived from the owning block when placed and restored from NBT on load
    public Mode BlockMode { get; private set; } = Mode.Impulse;

    public void SetCommand(string command) => Command = command;

    public void SetTrackOutput(bool value) => TrackOutput = value;

    public void SetConditional(bool value) => Conditional = value;

    public void SetAutomatic(bool value)
    {
        Automatic = value;
        //Configuration with auto on runs once without waiting for a redstone edge, maps to vanilla setAutomatic followed by trigger
        if (value) _executionScheduled = true;
    }

    public void SetMode(Mode mode) => BlockMode = mode;

    //SetPowered records the redstone state and schedules an execution on the rising edge
    //Repeating blocks run every tick while powered and ignore the schedule
    public void SetPowered(bool powered)
    {
        if (powered && !Powered) _executionScheduled = true;
        Powered = powered;
    }

    //SetAutomatic early flag used by the placement path before the first tick
    public void ScheduleExecution() => _executionScheduled = true;

    public override void Tick()
    {
        if (Level is null) return;
        //command_blocks_work off disables execution entirely, matching vanilla
        if (Level.GameRules is { } rules
            && rules.GetInt(NacreCraft.Game.World.Level.GameRules.CommandBlocksWork) == 0)
        {
            _executionScheduled = false;
            return;
        }

        if (BlockMode == Mode.Repeating)
        {
            if (Powered || Automatic) Execute();
            return;
        }

        if (!_executionScheduled) return;
        _executionScheduled = false;
        Execute();
    }

    //Execute runs the configured command through the injected executor and stores the result
    private void Execute()
    {
        if (Executor is null || string.IsNullOrWhiteSpace(Command)) return;
        var (success, output) = Executor(Level!, Pos, Command);
        SuccessCount = success;
        if (!TrackOutput) return;
        LastOutput = output;
        //Push the updated output to clients watching the block, maps to the vanilla block entity update packet
        Level!.BlockEntityChanged(Pos);
    }

    public override void SaveAdditional(CompoundTag tag)
    {
        tag.PutString("Command", Command);
        tag.PutBoolean("TrackOutput", TrackOutput);
        tag.PutBoolean("conditional", Conditional);
        tag.PutBoolean("auto", Automatic);
        tag.PutBoolean("powered", Powered);
        tag.PutInt("SuccessCount", SuccessCount);
        tag.PutInt("Mode", (int)BlockMode);
        if (LastOutput.Length > 0) tag.PutString("LastOutput", LastOutput);
    }

    public override void LoadAdditional(CompoundTag tag)
    {
        Command = tag.GetString("Command")?.Value ?? "";
        TrackOutput = tag.GetBooleanOr("TrackOutput", true);
        Conditional = tag.GetBooleanOr("conditional", false);
        Automatic = tag.GetBooleanOr("auto", false);
        Powered = tag.GetBooleanOr("powered", false);
        SuccessCount = tag.GetIntOr("SuccessCount", 0);
        BlockMode = (Mode)tag.GetIntOr("Mode", 0);
        LastOutput = tag.GetString("LastOutput")?.Value ?? "";
    }
}
