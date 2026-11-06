using NacreCraft.Client.Level;
using NacreCraft.Client.Player;
using NacreCraft.Primitives;
using NacreCraft.Registry.State;

namespace NacreCraft.Client.Multiplayer.Prediction;

//BlockStatePredictionHandler keeps the server-verified state of blocks the client changed predictively, maps to vanilla BlockStatePredictionHandler
//When the matching server sequence comes back the known state is applied again, so a rejected prediction snaps back
public sealed class BlockStatePredictionHandler : IDisposable
{
    private readonly Dictionary<long, ServerVerifiedState> _serverVerifiedStates = new();
    private int _currentSequenceNr;
    private bool _isPredicting;
    private int _lastTeleportSequence = -1;

    public void RetainKnownServerState(BlockPos pos, BlockState state, LocalPlayer player)
    {
        var key = pos.AsLong();
        if (_serverVerifiedStates.TryGetValue(key, out var existing))
            existing.Sequence = _currentSequenceNr;
        else
            _serverVerifiedStates[key] = new ServerVerifiedState(_currentSequenceNr, state, player.Pos);
    }

    public bool UpdateKnownServerState(BlockPos pos, BlockState blockState)
    {
        if (!_serverVerifiedStates.TryGetValue(pos.AsLong(), out var state)) return false;
        state.BlockState = blockState;
        return true;
    }

    //EndPredictionsUpTo applies every state at or below the acknowledged sequence and drops it from the table
    public void EndPredictionsUpTo(int sequence, ClientLevel clientLevel)
    {
        var keys = _serverVerifiedStates.Keys.Where(key => _serverVerifiedStates[key].Sequence <= sequence).ToList();
        foreach (var key in keys)
        {
            var state = _serverVerifiedStates[key];
            _serverVerifiedStates.Remove(key);
            clientLevel.SyncBlockState(BlockPos.FromLong(key), state.BlockState,
                _lastTeleportSequence < sequence ? state.PlayerPos : null);
        }
    }

    public BlockStatePredictionHandler StartPredicting()
    {
        _currentSequenceNr++;
        _isPredicting = true;
        return this;
    }

    public void Dispose() => _isPredicting = false;

    public int CurrentSequence() => _currentSequenceNr;

    public void OnTeleport() => _lastTeleportSequence = _currentSequenceNr;

    public bool IsPredicting() => _isPredicting;

    //ServerVerifiedState the last server-confirmed state plus the player position that made the prediction
    private sealed class ServerVerifiedState
    {
        public ServerVerifiedState(int sequence, BlockState blockState, Vec3 playerPos)
        {
            Sequence = sequence;
            BlockState = blockState;
            PlayerPos = playerPos;
        }

        public int Sequence { get; set; }

        public BlockState BlockState { get; set; }

        public Vec3 PlayerPos { get; }
    }
}
