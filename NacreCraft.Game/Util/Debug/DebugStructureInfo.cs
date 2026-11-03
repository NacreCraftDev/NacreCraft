using NacreCraft.Game.World.Level.LevelGen;
using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Util.Debug;

//DebugStructureInfo structure debug payload, maps to vanilla net.minecraft.util.debug.DebugStructureInfo
//Vanilla uses BoundingBox; NC names the same integer box BoundingBoxInt
public sealed record DebugStructureInfo(BoundingBoxInt BoundingBox, List<DebugStructureInfo.Piece> Pieces)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugStructureInfo> StreamCodec { get; } = new StructureInfoCodec();

    //Piece one structure piece box, maps to vanilla DebugStructureInfo.Piece
    public sealed record Piece(BoundingBoxInt BoundingBox, bool IsStart)
    {
        public static StreamCodec<RegistryFriendlyByteBuf, Piece> StreamCodec { get; } = new PieceCodec();
    }
}

internal sealed class StructureInfoCodec : StreamCodec<RegistryFriendlyByteBuf, DebugStructureInfo>
{
    public DebugStructureInfo Decode(RegistryFriendlyByteBuf buf)
    {
        var box = BoxCodec.Read(buf);
        var count = buf.ReadVarInt();
        var pieces = new List<DebugStructureInfo.Piece>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) pieces.Add(DebugStructureInfo.Piece.StreamCodec.Decode(buf));
        return new DebugStructureInfo(box, pieces);
    }

    public void Encode(RegistryFriendlyByteBuf buf, DebugStructureInfo value)
    {
        BoxCodec.Write(buf, value.BoundingBox);
        buf.WriteVarInt(value.Pieces.Count);
        foreach (var piece in value.Pieces) DebugStructureInfo.Piece.StreamCodec.Encode(buf, piece);
    }
}

internal sealed class PieceCodec : StreamCodec<RegistryFriendlyByteBuf, DebugStructureInfo.Piece>
{
    public DebugStructureInfo.Piece Decode(RegistryFriendlyByteBuf buf)
        => new(BoxCodec.Read(buf), buf.ReadBoolean());

    public void Encode(RegistryFriendlyByteBuf buf, DebugStructureInfo.Piece value)
    {
        BoxCodec.Write(buf, value.BoundingBox);
        buf.WriteBoolean(value.IsStart);
    }
}

//BoxCodec integer bounding box as two block positions, maps to vanilla BoundingBox.STREAM_CODEC
internal static class BoxCodec
{
    public static BoundingBoxInt Read(RegistryFriendlyByteBuf buf)
    {
        var min = buf.ReadBlockPos();
        var max = buf.ReadBlockPos();
        return new BoundingBoxInt(min.X, min.Y, min.Z, max.X, max.Y, max.Z);
    }

    public static void Write(RegistryFriendlyByteBuf buf, BoundingBoxInt box)
    {
        buf.WriteBlockPos(new BlockPos(box.MinX, box.MinY, box.MinZ));
        buf.WriteBlockPos(new BlockPos(box.MaxX, box.MaxY, box.MaxZ));
    }
}
