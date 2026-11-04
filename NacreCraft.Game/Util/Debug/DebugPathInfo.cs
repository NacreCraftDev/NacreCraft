using NacreCraft.Network;
using Path = NacreCraft.Game.World.Pathfinder.Path;

namespace NacreCraft.Game.Util.Debug;

//DebugPathInfo entity path debug payload, maps to vanilla net.minecraft.util.debug.DebugPathInfo
public sealed record DebugPathInfo(Path Path, float MaxNodeDistance)
{
    public static StreamCodec<FriendlyByteBuf, DebugPathInfo> StreamCodec { get; } = new PathInfoCodec();
}

internal sealed class PathInfoCodec : StreamCodec<FriendlyByteBuf, DebugPathInfo>
{
    public DebugPathInfo Decode(FriendlyByteBuf buf)
        => new(Path.StreamCodec.Decode(buf), buf.ReadFloat());

    public void Encode(FriendlyByteBuf buf, DebugPathInfo value)
    {
        Path.StreamCodec.Encode(buf, value.Path);
        buf.WriteFloat(value.MaxNodeDistance);
    }
}
