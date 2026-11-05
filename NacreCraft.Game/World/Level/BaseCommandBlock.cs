using NacreCraft.Network.Chat;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level;

//BaseCommandBlock command block state shared by the block entity and the minecart, maps to vanilla net.minecraft.world.level.BaseCommandBlock
//performCommand and createCommandSourceStack are omitted: the command source contract and the level command dispatcher are not wired here
public abstract class BaseCommandBlock
{
    private static readonly Component DefaultName = Component.Literal("@");

    private int _successCount;
    private Component? _lastOutput;
    private Component? _customName;
    private long _lastExecution = -1;
    private bool _updateLastExecution = true;
    private bool _trackOutput = true;
    private string _command = "";

    //OnUpdated notifies the owner that the stored output changed, maps to vanilla onUpdated
    public abstract void OnUpdated(ServerLevel level);

    //IsValid whether the owner still exists, maps to vanilla isValid
    public abstract bool IsValid();

    public int SuccessCount => _successCount;

    public void SetSuccessCount(int successCount) => _successCount = successCount;

    public Component LastOutput => _lastOutput ?? CommonComponents.Empty;

    public void SetLastOutput(Component? lastOutput) => _lastOutput = lastOutput;

    public Component Name => _customName ?? DefaultName;

    public Component? CustomName => _customName;

    public void SetCustomName(Component? name) => _customName = name;

    public string Command => _command;

    public void SetCommand(string command)
    {
        _command = command;
        _successCount = 0;
    }

    public bool TrackOutput => _trackOutput;

    public void SetTrackOutput(bool trackOutput) => _trackOutput = trackOutput;

    public bool UpdateLastExecution => _updateLastExecution;

    public void SetUpdateLastExecution(bool updateLastExecution) => _updateLastExecution = updateLastExecution;

    public long LastExecution => _lastExecution;

    public void SetLastExecution(long lastExecution) => _lastExecution = lastExecution;

    //Save writes the command block fields, maps to vanilla save
    public void Save(ValueOutput output)
    {
        output.PutString("Command", _command);
        output.PutInt("SuccessCount", _successCount);
        output.StoreNullable("CustomName", ComponentSerialization.Codec, _customName);
        output.PutBoolean("TrackOutput", _trackOutput);
        if (_trackOutput)
            output.StoreNullable("LastOutput", ComponentSerialization.Codec, _lastOutput);
        output.PutBoolean("UpdateLastExecution", _updateLastExecution);
        if (_updateLastExecution && _lastExecution != -1)
            output.PutLong("LastExecution", _lastExecution);
    }

    //Load restores the command block fields, maps to vanilla load
    public void Load(ValueInput input)
    {
        _command = input.GetStringOr("Command", "");
        _successCount = input.GetIntOr("SuccessCount", 0);
        _customName = input.Read("CustomName", ComponentSerialization.Codec);
        _trackOutput = input.GetBooleanOr("TrackOutput", true);
        _lastOutput = _trackOutput ? input.Read("LastOutput", ComponentSerialization.Codec) : null;
        _updateLastExecution = input.GetBooleanOr("UpdateLastExecution", true);
        _lastExecution = _updateLastExecution ? input.GetLongOr("LastExecution", -1) : -1;
    }
}
