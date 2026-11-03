using NacreCraft.Network;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Util.Debug;

//DebugBrainDump brain dump debug payload, maps to vanilla net.minecraft.util.debug.DebugBrainDump
//Only the wire payload is ported; takeBrainDump needs the villager/warden/brain subsystems NC does not have yet
public sealed record DebugBrainDump(
    string Name,
    string Profession,
    int Xp,
    float Health,
    float MaxHealth,
    string Inventory,
    bool WantsGolem,
    int AngerLevel,
    List<string> Activities,
    List<string> Behaviors,
    List<string> Memories,
    List<string> Gossips,
    HashSet<BlockPos> Pois,
    HashSet<BlockPos> PotentialPois)
{
    public static StreamCodec<RegistryFriendlyByteBuf, DebugBrainDump> StreamCodec { get; } = new BrainDumpCodec();

    public bool HasPoi(BlockPos poiPos) => Pois.Contains(poiPos);

    public bool HasPotentialPoi(BlockPos poiPos) => PotentialPois.Contains(poiPos);
}

internal sealed class BrainDumpCodec : StreamCodec<RegistryFriendlyByteBuf, DebugBrainDump>
{
    public DebugBrainDump Decode(RegistryFriendlyByteBuf buf)
        => new(
            buf.ReadString(),
            buf.ReadString(),
            buf.ReadInt(),
            buf.ReadFloat(),
            buf.ReadFloat(),
            buf.ReadString(),
            buf.ReadBoolean(),
            buf.ReadInt(),
            ReadStringList(buf),
            ReadStringList(buf),
            ReadStringList(buf),
            ReadStringList(buf),
            ReadPosSet(buf),
            ReadPosSet(buf));

    public void Encode(RegistryFriendlyByteBuf buf, DebugBrainDump value)
    {
        buf.WriteString(value.Name);
        buf.WriteString(value.Profession);
        buf.WriteInt(value.Xp);
        buf.WriteFloat(value.Health);
        buf.WriteFloat(value.MaxHealth);
        buf.WriteString(value.Inventory);
        buf.WriteBoolean(value.WantsGolem);
        buf.WriteInt(value.AngerLevel);
        WriteStringList(buf, value.Activities);
        WriteStringList(buf, value.Behaviors);
        WriteStringList(buf, value.Memories);
        WriteStringList(buf, value.Gossips);
        WritePosSet(buf, value.Pois);
        WritePosSet(buf, value.PotentialPois);
    }

    private static List<string> ReadStringList(RegistryFriendlyByteBuf buf)
    {
        var count = buf.ReadVarInt();
        var list = new List<string>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) list.Add(buf.ReadString());
        return list;
    }

    private static void WriteStringList(RegistryFriendlyByteBuf buf, List<string> list)
    {
        buf.WriteVarInt(list.Count);
        foreach (var value in list) buf.WriteString(value);
    }

    private static HashSet<BlockPos> ReadPosSet(RegistryFriendlyByteBuf buf)
    {
        var count = buf.ReadVarInt();
        var set = new HashSet<BlockPos>();
        for (var i = 0; i < count; i++) set.Add(buf.ReadBlockPos());
        return set;
    }

    private static void WritePosSet(RegistryFriendlyByteBuf buf, HashSet<BlockPos> set)
    {
        buf.WriteVarInt(set.Count);
        foreach (var pos in set) buf.WriteBlockPos(pos);
    }
}
