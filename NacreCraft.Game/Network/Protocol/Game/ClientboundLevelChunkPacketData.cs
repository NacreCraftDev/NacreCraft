//ClientboundLevelChunkPacketData maps to vanilla net.minecraft.network.protocol.game.ClientboundLevelChunkPacketData
using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.Chunk;
using NacreCraft.Nbt;
using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Storage;
using NacreCraft.Storage.Paletted;
using HeightmapRegistry = NacreCraft.Registry.Heightmap;

namespace NacreCraft.Game.Network.Protocol.Game;

//Holds the chunk payload of ClientboundLevelChunkWithLightPacket: heightmaps, section bytes and block entity entries
//NacreCraft currently inlines this data in ClientboundLevelChunkWithLightPacket and LevelChunkSerializer; this type is the vanilla-shaped entry point
//The packet is intentionally left using the inline path, so adding this type does not change any send behavior
public sealed class ClientboundLevelChunkPacketData
{
    //TwoMegabytes read limit, maps to vanilla TWO_MEGABYTES
    private const int TwoMegabytes = 2097152;

    //Heightmaps heightmap data sent to the client, maps to vanilla heightmaps
    public IReadOnlyDictionary<HeightmapRegistry.Types, long[]> Heightmaps { get; }

    //Buffer serialized section bytes only, maps to vanilla buffer
    //The packet-level heightmap map and block entity list are kept as separate fields
    public byte[] Buffer { get; }

    //BlockEntitiesData block entity entries in chunk-local coordinates, maps to vanilla blockEntitiesData
    private readonly List<BlockEntityInfo> _blockEntitiesData;

    //BlockEntityTagOutput receives one decoded block entity entry, maps to vanilla BlockEntityTagOutput
    public delegate void BlockEntityTagOutput(BlockPos pos, BlockEntityType? type, CompoundTag? tag);

    //Builds the payload from a chunk, maps to the vanilla LevelChunk constructor
    //ChunkAccess is relaxed from vanilla LevelChunk because the send path still holds a ProtoChunk during generation,
    //the same relaxation ClientboundLevelChunkWithLightPacket already makes
    //blockEntities are the same full compounds the existing serializer consumes
    public ClientboundLevelChunkPacketData(ChunkAccess chunk, IReadOnlyList<CompoundTag>? blockEntities = null)
    {
        var heightmaps = new Dictionary<HeightmapRegistry.Types, long[]>();
        foreach (var (type, data) in chunk.Heightmaps)
        {
            if (SendToClient(type)) heightmaps[type] = (long[])data.Clone();
        }
        Heightmaps = heightmaps;
        Buffer = ExtractChunkBuffer(chunk);

        _blockEntitiesData = new List<BlockEntityInfo>(blockEntities?.Count ?? 0);
        if (blockEntities is null) return;
        foreach (var tag in blockEntities)
            _blockEntitiesData.Add(BlockEntityInfo.Create(tag));
    }

    //Reads a payload from the network, maps to the vanilla RegistryFriendlyByteBuf constructor
    //The vanilla x/z arguments are unused there and are omitted here
    public ClientboundLevelChunkPacketData(FriendlyByteBuf input)
    {
        var heightmapCount = input.ReadVarInt();
        var heightmaps = new Dictionary<HeightmapRegistry.Types, long[]>(heightmapCount);
        for (var i = 0; i < heightmapCount; i++)
        {
            var type = (HeightmapRegistry.Types)input.ReadVarInt();
            heightmaps[type] = input.ReadLongArray();
        }
        Heightmaps = heightmaps;

        var size = input.ReadVarInt();
        if (size > TwoMegabytes)
            throw new InvalidOperationException("Chunk packet trying to allocate too much memory on read.");
        Buffer = input.ReadBytes(size);

        var blockEntityCount = input.ReadVarInt();
        _blockEntitiesData = new List<BlockEntityInfo>(blockEntityCount);
        for (var i = 0; i < blockEntityCount; i++)
            _blockEntitiesData.Add(BlockEntityInfo.Read(input));
    }

    //Write writes heightmaps, the section buffer and block entity entries, maps to vanilla write
    public void Write(FriendlyByteBuf output)
    {
        output.WriteVarInt(Heightmaps.Count);
        foreach (var (type, data) in Heightmaps)
        {
            output.WriteVarInt((int)type);
            output.WriteLongArray(data);
        }

        output.WriteVarInt(Buffer.Length);
        output.WriteBytes(Buffer);

        output.WriteVarInt(_blockEntitiesData.Count);
        foreach (var data in _blockEntitiesData) data.Write(output);
    }

    //GetReadBuffer wraps the section bytes in a readable buffer, maps to vanilla getReadBuffer
    public FriendlyByteBuf GetReadBuffer() => new(Buffer);

    //GetBlockEntitiesTagsConsumer returns a consumer that resolves block entity positions into world coordinates, maps to vanilla getBlockEntitiesTagsConsumer
    public Action<BlockEntityTagOutput> GetBlockEntitiesTagsConsumer(int x, int z)
        => output => GetBlockEntitiesTags(output, x, z);

    //GetBlockEntitiesTags turns each chunk-local entry into a world position, maps to vanilla getBlockEntitiesTags
    private void GetBlockEntitiesTags(BlockEntityTagOutput output, int x, int z)
    {
        var baseX = 16 * x;
        var baseZ = 16 * z;
        foreach (var data in _blockEntitiesData)
        {
            var unpackedX = baseX + ((data.PackedXZ >> 4) & 15);
            var unpackedZ = baseZ + (data.PackedXZ & 15);
            output(new BlockPos(unpackedX, data.Y, unpackedZ), data.Type, data.Tag);
        }
    }

    //ExtractChunkBuffer reuses LevelChunkSerializer and strips the heightmap header and block entity tail it adds around the section bytes
    //This keeps one section codec instead of duplicating the paletted container format here
    private static byte[] ExtractChunkBuffer(ChunkAccess chunk)
    {
        var factory = PalettedContainerFactory.Default;
        var encoded = new FriendlyByteBuf(chunk.SectionsCount * 2048);
        LevelChunkSerializer.Write(encoded, chunk, factory.CreateForBlockStates, factory.CreateForBiomes);

        var reader = new FriendlyByteBuf(encoded.ToArray());
        var heightmapCount = reader.ReadVarInt();
        for (var i = 0; i < heightmapCount; i++)
        {
            reader.ReadVarInt();
            reader.ReadLongArray();
        }
        var size = reader.ReadVarInt();
        return reader.ReadBytes(size);
    }

    //SendToClient marks the heightmaps vanilla sends in the chunk packet, maps to vanilla Types.sendToClient
    private static bool SendToClient(HeightmapRegistry.Types type)
        => type is HeightmapRegistry.Types.WorldSurface
            or HeightmapRegistry.Types.MotionBlocking
            or HeightmapRegistry.Types.MotionBlockingNoLeaves;

    //BlockEntityInfo one block entity entry, maps to vanilla ClientboundLevelChunkPacketData$BlockEntityInfo
    //packedXZ is a byte with x in the high 4 bits and z in the low 4 bits
    private sealed record BlockEntityInfo(int PackedXZ, int Y, BlockEntityType? Type, CompoundTag? Tag)
    {
        //Create builds an entry from a block entity compound, maps to vanilla BlockEntityInfo.create
        //The compound carries id/x/y/z, the same shape LevelChunkSerializer writes today
        public static BlockEntityInfo Create(CompoundTag tag)
        {
            var x = tag.GetIntOr("x", 0);
            var y = tag.GetIntOr("y", 0);
            var z = tag.GetIntOr("z", 0);
            var type = tag.GetString("id")?.Value is { } key ? BlockEntityTypes.ByKey(key) : null;
            var packedXZ = ((x & 15) << 4) | (z & 15);
            return new BlockEntityInfo(packedXZ, y, type, tag);
        }

        //Read decodes one entry, maps to the vanilla codec constructor
        public static BlockEntityInfo Read(FriendlyByteBuf input)
        {
            var packedXZ = input.ReadByte();
            var y = input.ReadShort();
            var type = BlockEntityTypes.ById(input.ReadVarInt());
            var tag = input.ReadNbt() as CompoundTag;
            return new BlockEntityInfo(packedXZ, y, type, tag);
        }

        //Write encodes one entry, maps to the vanilla codec write
        public void Write(FriendlyByteBuf output)
        {
            output.WriteByte((byte)PackedXZ);
            output.WriteShort((short)Y);
            output.WriteVarInt(Type?.RawId ?? 0);
            output.WriteNbt(Tag);
        }
    }
}
