using NacreCraft.Primitives;

namespace NacreCraft.Storage;

//ChunkTrackingView which chunks a player receives updates for, maps to vanilla net.minecraft.server.level.ChunkTrackingView
//The view is a square around the player's chunk; entering or leaving it drives chunk packet sending
public readonly record struct ChunkTrackingView(ChunkPos Center, int ViewDistance)
{
    public static ChunkTrackingView Empty => new(default, -1);

    public bool IsEmpty => ViewDistance < 0;

    //Contains whether the chunk falls inside the view, maps to vanilla contains
    public bool Contains(ChunkPos pos)
    {
        if (IsEmpty) return false;
        var dx = Math.Abs(pos.X - Center.X);
        var dz = Math.Abs(pos.Z - Center.Z);
        return dx <= ViewDistance && dz <= ViewDistance;
    }

    //Contains with a chunk coordinate pair
    public bool Contains(int chunkX, int chunkZ) => Contains(new ChunkPos(chunkX, chunkZ));
}
