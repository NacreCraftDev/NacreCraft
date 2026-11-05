using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//BlockDestructionProgress one block being broken by a player, maps to vanilla net.minecraft.server.level.BlockDestructionProgress
//The server tracks progress per position and broadcasts it to nearby clients, which render the cracking overlay
public sealed class BlockDestructionProgress(int id, BlockPos pos)
{
    private readonly HashSet<int> _affectedPlayers = new();

    public int Id { get; } = id;

    public BlockPos Pos { get; } = pos;

    //Progress 0..9 cracking stage
    public int Progress { get; private set; }

    //UpdatedRenderTick last tick the progress was sent, used to expire stale entries
    public long UpdatedRenderTick { get; private set; }

    public IReadOnlyCollection<int> AffectedPlayers => _affectedPlayers;

    public void SetProgress(int progress) => Progress = progress;

    public void AddPlayer(int playerId) => _affectedPlayers.Add(playerId);

    public void RemovePlayer(int playerId) => _affectedPlayers.Remove(playerId);

    //UpdateTick records the send tick, maps to vanilla updateTick
    public bool UpdateTick(long tick)
    {
        if (UpdatedRenderTick >= tick) return false;
        UpdatedRenderTick = tick;
        return true;
    }
}
