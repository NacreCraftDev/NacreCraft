using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.World.Pathfinder;

//Path a computed path, maps to vanilla net.minecraft.world.level.pathfinder.Path
//The pathfinder subsystem is not ported yet, so this keeps only the parts the debug payload needs: the node positions,
//the target, whether the target was reached and the next node index
public sealed record Path(IReadOnlyList<BlockPos> Nodes, BlockPos Target, bool Reached, int NextNodeIndex = 0)
{
    //StreamCodec wire form, maps to vanilla Path.STREAM_CODEC
    //Vanilla writes richer nodes plus the debug data; the reduced form writes the node positions only
    public static readonly StreamCodec<FriendlyByteBuf, Path> StreamCodec = new PathStreamCodec();
}

//PathStreamCodec reduced path payload
internal sealed class PathStreamCodec : StreamCodec<FriendlyByteBuf, Path>
{
    public Path Decode(FriendlyByteBuf buf)
    {
        var reached = buf.ReadBoolean();
        var nextNodeIndex = buf.ReadInt();
        var target = buf.ReadBlockPos();
        var count = buf.ReadVarInt();
        var nodes = new List<BlockPos>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) nodes.Add(buf.ReadBlockPos());
        return new Path(nodes, target, reached, nextNodeIndex);
    }

    public void Encode(FriendlyByteBuf buf, Path value)
    {
        buf.WriteBoolean(value.Reached);
        buf.WriteInt(value.NextNodeIndex);
        buf.WriteBlockPos(value.Target);
        buf.WriteVarInt(value.Nodes.Count);
        foreach (var node in value.Nodes) buf.WriteBlockPos(node);
    }
}
