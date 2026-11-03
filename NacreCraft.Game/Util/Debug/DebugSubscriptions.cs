using NacreCraft.Network;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Util.Debug;

//DebugSubscriptions built-in debug subscriptions, maps to vanilla net.minecraft.util.debug.DebugSubscriptions
//Registration order defines the registry ids both sides use in the debug value packets
public static class DebugSubscriptions
{
    public static readonly DebugSubscription DedicatedServerTickTime = RegisterSimple("dedicated_server_tick_time");
    public static readonly DebugSubscription<DebugBeeInfo> Bees = RegisterWithValue("bees", DebugBeeInfo.StreamCodec);
    public static readonly DebugSubscription<DebugBrainDump> Brains = RegisterWithValue("brains", DebugBrainDump.StreamCodec);
    public static readonly DebugSubscription<DebugBreezeInfo> Breezes = RegisterWithValue("breezes", DebugBreezeInfo.StreamCodec);
    public static readonly DebugSubscription<DebugGoalInfo> GoalSelectors = RegisterWithValue("goal_selectors", DebugGoalInfo.StreamCodec);
    //Entity paths need the pathfinder Path payload, which NC does not have yet, so this stays a value-less subscription
    public static readonly DebugSubscription EntityPaths = RegisterSimple("entity_paths");
    public static readonly DebugSubscription<DebugEntityBlockIntersection> EntityBlockIntersections =
        RegisterTemporaryValue("entity_block_intersections", DebugEntityBlockIntersectionCodec.Instance, 100);
    public static readonly DebugSubscription<DebugHiveInfo> BeeHives = RegisterWithValue("bee_hives", DebugHiveInfo.StreamCodec);
    public static readonly DebugSubscription<DebugPoiInfo> Pois = RegisterWithValue("pois", DebugPoiInfo.StreamCodec);
    //Redstone wire orientations need the redstone Orientation type, which NC does not have yet
    public static readonly DebugSubscription RedstoneWireOrientations = RegisterSimple("redstone_wire_orientations");
    public static readonly DebugSubscription<Unit> VillageSections = RegisterWithValue("village_sections", Unit.StreamCodec);
    public static readonly DebugSubscription<List<BlockPos>> Raids = RegisterWithValue("raids", new BlockPosListCodec());
    public static readonly DebugSubscription<List<DebugStructureInfo>> Structures =
        RegisterWithValue("structures", new StructureInfoListCodec());
    public static readonly DebugSubscription<DebugGameEventListenerInfo> GameEventListeners =
        RegisterWithValue("game_event_listeners", DebugGameEventListenerInfo.StreamCodec);
    public static readonly DebugSubscription<BlockPos> NeighborUpdates =
        RegisterTemporaryValue("neighbor_updates", new BlockPosCodec(), 200);
    public static readonly DebugSubscription<DebugGameEventInfo> GameEvents =
        RegisterTemporaryValue("game_events", DebugGameEventInfo.StreamCodec, 60);

    //Bootstrap fills the registry and returns the entry vanilla reports as its bootstrap result
    public static DebugSubscription Bootstrap()
    {
        //Touching the type initializer above is what registers every entry
        return DedicatedServerTickTime;
    }

    private static DebugSubscription RegisterSimple(string id)
    {
        var subscription = new DebugSubscription(null);
        Register(id, subscription);
        return subscription;
    }

    private static DebugSubscription<T> RegisterWithValue<T>(string id, StreamCodec<RegistryFriendlyByteBuf, T> valueStreamCodec)
    {
        var subscription = new DebugSubscription<T>(valueStreamCodec);
        Register(id, subscription);
        return subscription;
    }

    private static DebugSubscription<T> RegisterTemporaryValue<T>(string id, StreamCodec<RegistryFriendlyByteBuf, T> valueStreamCodec, int expireAfterTicks)
    {
        var subscription = new DebugSubscription<T>(valueStreamCodec, expireAfterTicks);
        Register(id, subscription);
        return subscription;
    }

    private static void Register(string id, DebugSubscription subscription)
        => Registry<object>.Register(BuiltInRegistries.DEBUG_SUBSCRIPTION, Identifier.WithDefaultNamespace(id), subscription);
}

//BlockPosCodec block position as a packed long, maps to vanilla BlockPos.STREAM_CODEC
internal sealed class BlockPosCodec : StreamCodec<RegistryFriendlyByteBuf, BlockPos>
{
    public BlockPos Decode(RegistryFriendlyByteBuf buf) => buf.ReadBlockPos();

    public void Encode(RegistryFriendlyByteBuf buf, BlockPos value) => buf.WriteBlockPos(value);
}

//BlockPosListCodec list of block positions, maps to BlockPos.STREAM_CODEC.apply(ByteBufCodecs.list())
internal sealed class BlockPosListCodec : StreamCodec<RegistryFriendlyByteBuf, List<BlockPos>>
{
    private static readonly BlockPosCodec ElementCodec = new();

    public List<BlockPos> Decode(RegistryFriendlyByteBuf buf)
    {
        var count = buf.ReadVarInt();
        var list = new List<BlockPos>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) list.Add(ElementCodec.Decode(buf));
        return list;
    }

    public void Encode(RegistryFriendlyByteBuf buf, List<BlockPos> value)
    {
        buf.WriteVarInt(value.Count);
        foreach (var pos in value) ElementCodec.Encode(buf, pos);
    }
}

//StructureInfoListCodec list of structure debug payloads, maps to DebugStructureInfo.STREAM_CODEC.apply(ByteBufCodecs.list())
internal sealed class StructureInfoListCodec : StreamCodec<RegistryFriendlyByteBuf, List<DebugStructureInfo>>
{
    public List<DebugStructureInfo> Decode(RegistryFriendlyByteBuf buf)
    {
        var count = buf.ReadVarInt();
        var list = new List<DebugStructureInfo>(Math.Min(count, ByteBufCodecs.MaxInitialCollectionSize));
        for (var i = 0; i < count; i++) list.Add(DebugStructureInfo.StreamCodec.Decode(buf));
        return list;
    }

    public void Encode(RegistryFriendlyByteBuf buf, List<DebugStructureInfo> value)
    {
        buf.WriteVarInt(value.Count);
        foreach (var info in value) DebugStructureInfo.StreamCodec.Encode(buf, info);
    }
}
