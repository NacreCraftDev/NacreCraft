using NacreCraft.Game.World.Items;
using NacreCraft.Network;
using NacreCraft.Network.Chat;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Registry.Enums;

namespace NacreCraft.Game.Network.Protocol.Game;

//IEntityDataSerializer non-generic serializer view used by the metadata codec
//Maps to vanilla EntityDataSerializer; each value type has its own typed instance registered under a fixed id
public interface IEntityDataSerializer
{
    int Id { get; }
    object Decode(RegistryFriendlyByteBuf buf);
    void Encode(RegistryFriendlyByteBuf buf, object value);
    object Copy(object value);
}

//EntityDataSerializer typed serializer, maps to vanilla EntityDataSerializer
//Copy isolates the stored value from the caller's stack/object, matching vanilla serializer.copy on DataValue creation
public sealed class EntityDataSerializer<T> : IEntityDataSerializer
{
    public int Id { get; }

    private readonly Func<RegistryFriendlyByteBuf, T> _decode;
    private readonly Action<RegistryFriendlyByteBuf, T> _encode;
    private readonly Func<T, T> _copy;

    public EntityDataSerializer(int id, Func<RegistryFriendlyByteBuf, T> decode,
        Action<RegistryFriendlyByteBuf, T> encode, Func<T, T>? copy = null)
    {
        Id = id;
        _decode = decode;
        _encode = encode;
        _copy = copy ?? (value => value);
    }

    //CreateAccessor binds a metadata index to this serializer
    public EntityDataAccessor<T> CreateAccessor(byte index) => new(index, Id);

    object IEntityDataSerializer.Decode(RegistryFriendlyByteBuf buf) => _decode(buf)!;

    void IEntityDataSerializer.Encode(RegistryFriendlyByteBuf buf, object value) => _encode(buf, (T)value);

    object IEntityDataSerializer.Copy(object value) => _copy((T)value)!;
}

//EntityDataSerializers entity data serializer registry, maps to vanilla EntityDataSerializers
//Ids follow the vanilla registration order exactly; an unregistered id means the following bytes cannot be located, so it throws
public static class EntityDataSerializers
{
    //Ids 0-20 cover the vanilla prefix implemented here; later variant serializers stay unregistered until their subsystems land
    public const int Byte = 0;
    public const int IntId = 1;
    public const int LongId = 2;
    public const int FloatId = 3;
    public const int StringId = 4;
    public const int ComponentId = 5;
    public const int OptionalComponentId = 6;
    public const int ItemStackId = 7;
    public const int BooleanId = 8;
    public const int BlockPosId = 10;
    public const int OptionalBlockPosId = 11;
    public const int DirectionId = 12;
    public const int BlockStateId = 14;
    public const int OptionalBlockStateId = 15;
    public const int OptionalUnsignedIntId = 19;
    public const int Pose = 20;

    private static readonly Dictionary<int, IEntityDataSerializer> ById = new();

    static EntityDataSerializers()
    {
        Register(new EntityDataSerializer<byte>(Byte,
            buf => buf.ReadByte(), (buf, value) => buf.WriteByte(value)));
        Register(new EntityDataSerializer<int>(IntId,
            buf => buf.ReadVarInt(), (buf, value) => buf.WriteVarInt(value)));
        Register(new EntityDataSerializer<long>(LongId,
            buf => buf.ReadVarLong(), (buf, value) => buf.WriteVarLong(value)));
        Register(new EntityDataSerializer<float>(FloatId,
            buf => buf.ReadFloat(), (buf, value) => buf.WriteFloat(value)));
        Register(new EntityDataSerializer<string>(StringId,
            buf => buf.ReadString(), (buf, value) => buf.WriteString(value)));
        Register(new EntityDataSerializer<Component>(ComponentId,
            buf => ComponentSerialization.StreamCodec.Decode(buf),
            (buf, value) => ComponentSerialization.StreamCodec.Encode(buf, value)));
        Register(new EntityDataSerializer<Component?>(OptionalComponentId,
            buf => buf.ReadBoolean() ? ComponentSerialization.StreamCodec.Decode(buf) : null,
            (buf, value) =>
            {
                buf.WriteBoolean(value is not null);
                if (value is not null) ComponentSerialization.StreamCodec.Encode(buf, value);
            }));
        Register(new EntityDataSerializer<ItemStack>(ItemStackId,
            buf => ItemStack.OptionalStreamCodec.Decode(buf),
            (buf, value) => ItemStack.OptionalStreamCodec.Encode(buf, value),
            value => value.Copy()));
        Register(new EntityDataSerializer<bool>(BooleanId,
            buf => buf.ReadBoolean(), (buf, value) => buf.WriteBoolean(value)));
        Register(new EntityDataSerializer<BlockPos>(BlockPosId,
            buf => buf.ReadBlockPos(), (buf, value) => buf.WriteBlockPos(value)));
        Register(new EntityDataSerializer<BlockPos?>(OptionalBlockPosId,
            buf => buf.ReadBoolean() ? buf.ReadBlockPos() : null,
            (buf, value) =>
            {
                buf.WriteBoolean(value.HasValue);
                if (value.HasValue) buf.WriteBlockPos(value.Value);
            }));
        Register(new EntityDataSerializer<NacreCraft.Registry.Enums.Direction>(DirectionId,
            buf => (NacreCraft.Registry.Enums.Direction)buf.ReadVarInt(),
            (buf, value) => buf.WriteVarInt((int)value)));
        //BlockState travels as its global state id, maps to vanilla Block.getId/stateById
        Register(new EntityDataSerializer<int>(BlockStateId,
            buf => buf.ReadVarInt(), (buf, value) => buf.WriteVarInt(value)));
        //Optional block state uses 0 for empty and the global state id otherwise, matching vanilla's OPTIONAL_BLOCK_STATE_CODEC
        Register(new EntityDataSerializer<int>(OptionalBlockStateId,
            buf => buf.ReadVarInt(), (buf, value) => buf.WriteVarInt(value < 0 ? 0 : value)));
        //Optional unsigned int uses 0 for empty and value+1 otherwise, matching vanilla's OPTIONAL_UNSIGNED_INT_CODEC
        Register(new EntityDataSerializer<int>(OptionalUnsignedIntId,
            buf =>
            {
                var raw = buf.ReadVarInt();
                return raw == 0 ? -1 : raw - 1;
            },
            (buf, value) => buf.WriteVarInt(value < 0 ? 0 : value + 1)));
        Register(new EntityDataSerializer<int>(Pose,
            buf => buf.ReadVarInt(), (buf, value) => buf.WriteVarInt(value)));
    }

    //Register adds a serializer under its own id; a duplicate id replaces the previous entry only during startup wiring
    public static void Register<T>(EntityDataSerializer<T> serializer) => ById[serializer.Id] = serializer;

    //Get returns the serializer for an id, null when this build has not registered it
    public static IEntityDataSerializer? Get(int id) => ById.GetValueOrDefault(id);

    //Decode reads one value by serializer id
    public static object Decode(int id, RegistryFriendlyByteBuf buf)
        => Get(id)?.Decode(buf)
           ?? throw new NotSupportedException($"Unimplemented entity data serializer id {id}");

    //Encode writes one value by serializer id
    public static void Encode(int id, RegistryFriendlyByteBuf buf, object value)
    {
        var serializer = Get(id)
            ?? throw new NotSupportedException($"Unimplemented entity data serializer id {id}");
        serializer.Encode(buf, value);
    }
}
