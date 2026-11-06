using NacreCraft.Codec;
using NacreCraft.Game.Server;
using NacreCraft.Nbt;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using NacreCraft.Storage;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.Server.BossEvents;

//CustomBossEvents the saved set of command-created boss bars, maps to vanilla net.minecraft.server.bossevents.CustomBossEvents
//NC's SavedData persists CompoundTag directly, so the vanilla codec is applied per entry through NbtOps
public sealed class CustomBossEvents : SavedData
{
    public const string TypeId = "custom_boss_events";

    private readonly Dictionary<Identifier, CustomBossEvent> _events = new();

    public override string Id => TypeId;

    public CustomBossEvent? Get(Identifier id) => _events.GetValueOrDefault(id);

    public CustomBossEvent Create(RandomSource random, Identifier id, Component name)
    {
        var result = new CustomBossEvent(id, name, SetDirty);
        _events[id] = result;
        SetDirty();
        return result;
    }

    public void Remove(CustomBossEvent bossEvent)
    {
        if (_events.Remove(bossEvent.CustomId)) SetDirty();
    }

    public IReadOnlyCollection<Identifier> GetIds() => _events.Keys;

    public IReadOnlyCollection<CustomBossEvent> GetEvents() => _events.Values;

    public void OnPlayerConnect(ServerPlayer player)
    {
        foreach (var bossEvent in _events.Values) bossEvent.OnPlayerConnect(player);
    }

    public void OnPlayerDisconnect(ServerPlayer player)
    {
        foreach (var bossEvent in _events.Values) bossEvent.OnPlayerDisconnect(player);
    }

    public override CompoundTag Save(CompoundTag tag)
    {
        var list = new ListTag();
        foreach (var (id, bossEvent) in _events)
        {
            var entry = new CompoundTag();
            entry.PutString("id", id.ToString());
            entry.Put("data", CustomBossEvent.Packed.CODEC
                .EncodeStart(NbtOps.Instance, bossEvent.Pack()).GetOrThrow());
            list.Add(entry);
        }
        tag.Put("events", list);
        ClearDirty();
        return tag;
    }

    public static readonly SavedDataType<CustomBossEvents> TYPE = new CustomBossEventsType();

    private sealed class CustomBossEventsType : SavedDataType<CustomBossEvents>
    {
        public string Id => TypeId;

        public CustomBossEvents Create(CompoundTag tag, RegistryAccess registryAccess)
        {
            var result = new CustomBossEvents();
            var list = tag.GetList("events");
            if (list is null) return result;
            foreach (var element in list)
            {
                if (element is not CompoundTag entry) continue;
                var idText = entry.GetString("id")?.Value;
                if (idText is null || Identifier.TryParse(idText) is not { } id) continue;
                if (entry.GetCompound("data") is not { } data) continue;
                var packed = CustomBossEvent.Packed.CODEC.Parse(NbtOps.Instance, data).Result();
                if (!packed.IsPresent) continue;
                result._events[id] = CustomBossEvent.Load(id, packed.Get(), result.SetDirty);
            }
            return result;
        }
    }
}
