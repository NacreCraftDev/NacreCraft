using NetCraft.Network;
using NetCraft.Primitives;
using NetCraft.Registry;

namespace NetCraft.Game.World.Level.GameEvents;

//PositionSource where a game event was produced, maps to vanilla net.minecraft.world.level.gameevent.PositionSource
//The source is resolved on demand because the entity forms cannot be looked up until the level is available
public interface PositionSource
{
    //StreamCodec codec writing the registry id of the type followed by the type specific fields
    public static readonly StreamCodec<RegistryFriendlyByteBuf, PositionSource> StreamCodec = new PositionSourceStreamCodec();

    //GetPosition the current position, null when the source cannot be resolved right now
    Vec3? GetPosition(ILevelReader level);

    //Type the registered type of this source, which decides how it is encoded
    PositionSourceType Type { get; }
}

//PositionSourceType type of a position source, non-generic half so the registry can hold every kind
//The type registry in Registry is typed over object; this class is what actually gets registered there
public abstract class PositionSourceType : NetCraft.Registry.PositionSourceType<object>
{
    //WriteParameters writes the fields specific to this source type
    public abstract void WriteParameters(RegistryFriendlyByteBuf buf, PositionSource source);

    //ReadParameters reads the fields specific to this source type back
    public abstract PositionSource ReadParameters(RegistryFriendlyByteBuf buf);
}

//PositionSourceType<T> typed half, maps to vanilla PositionSourceType<T>
//Lets each concrete type work with its own source class instead of the interface
public abstract class PositionSourceType<T> : PositionSourceType where T : PositionSource
{
    public override void WriteParameters(RegistryFriendlyByteBuf buf, PositionSource source)
        => WriteTyped(buf, (T)source);

    public override PositionSource ReadParameters(RegistryFriendlyByteBuf buf) => ReadTyped(buf);

    protected abstract void WriteTyped(RegistryFriendlyByteBuf buf, T source);

    protected abstract T ReadTyped(RegistryFriendlyByteBuf buf);
}

//BlockPositionSource fixed block position, maps to vanilla BlockPositionSource
public sealed record BlockPositionSource(BlockPos Pos) : PositionSource
{
    public Vec3? GetPosition(ILevelReader level) => new(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);

    public PositionSourceType Type => PositionSourceTypes.Block;
}

//BlockPositionSourceType block position encoding, maps to vanilla BlockPositionSource.Type
public sealed class BlockPositionSourceType : PositionSourceType<BlockPositionSource>
{
    protected override void WriteTyped(RegistryFriendlyByteBuf buf, BlockPositionSource source)
        => buf.WriteBlockPos(source.Pos);

    protected override BlockPositionSource ReadTyped(RegistryFriendlyByteBuf buf)
        => new(buf.ReadBlockPos());
}

//EntityPositionSource an entity position, maps to vanilla EntityPositionSource
//Vanilla holds either the entity itself or a uuid/id to resolve later; the resolution needs a level that can look
//entities up, which does not exist yet, so an unresolved source reports no position instead of throwing
public sealed class EntityPositionSource : PositionSource
{
    private NetCraft.Registry.Entity? _entity;
    private readonly Guid _uuid;
    private readonly int _entityId;

    private readonly float _yOffset;

    public EntityPositionSource(NetCraft.Registry.Entity entity, float yOffset)
    {
        _entity = entity;
        _uuid = Guid.Empty;
        _entityId = -1;
        _yOffset = yOffset;
    }

    //EntityPositionSource from a uuid, resolved on first position lookup once an entity lookup exists, maps to vanilla Either.right(left)
    public EntityPositionSource(Guid uuid, float yOffset)
    {
        _uuid = uuid;
        _entityId = -1;
        _yOffset = yOffset;
    }

    //EntityPositionSource from an entity id, resolved the same way, maps to vanilla Either.right(right)
    public EntityPositionSource(int entityId, float yOffset)
    {
        _uuid = Guid.Empty;
        _entityId = entityId;
        _yOffset = yOffset;
    }

    //Uuid the source entity uuid, only meaningful when the source came from or resolved to an entity
    public Guid Uuid => _entity?.Uuid ?? _uuid;

    //YOffset vertical offset applied on top of the entity position
    public float YOffset => _yOffset;

    public Vec3? GetPosition(ILevelReader level)
        => _entity is null ? null : _entity.Pos.Add(0.0, _yOffset, 0.0);

    public PositionSourceType Type => PositionSourceTypes.Entity;
}

//EntityPositionSourceType entity position encoding, maps to vanilla EntityPositionSource.Type
public sealed class EntityPositionSourceType : PositionSourceType<EntityPositionSource>
{
    protected override void WriteTyped(RegistryFriendlyByteBuf buf, EntityPositionSource source)
    {
        buf.WriteUuid(source.Uuid);
        buf.WriteFloat(source.YOffset);
    }

    protected override EntityPositionSource ReadTyped(RegistryFriendlyByteBuf buf)
        => new(buf.ReadUuid(), buf.ReadFloat());
}

//PositionSourceTypes built-in position source types, maps to the static fields on vanilla PositionSourceType
//Registration order follows vanilla so the registry ids match the client
public static class PositionSourceTypes
{
    public static readonly BlockPositionSourceType Block = Register("block", new BlockPositionSourceType());

    public static readonly EntityPositionSourceType Entity = Register("entity", new EntityPositionSourceType());

    private static T Register<T>(string path, T type) where T : PositionSourceType
    {
        var id = Identifier.WithDefaultNamespace(path);
        Registry<NetCraft.Registry.PositionSourceType<object>>.Register(
            BuiltInRegistries.POSITION_SOURCE_TYPE,
            ResourceKey<NetCraft.Registry.PositionSourceType<object>>.Create(Registries.POSITION_SOURCE_TYPE, id),
            type);
        return type;
    }
}

//PositionSourceStreamCodec writes the type registry id followed by the type specific fields
//maps to the vanilla ByteBufCodecs.registry(...).dispatch(...) combination
internal sealed class PositionSourceStreamCodec : StreamCodec<RegistryFriendlyByteBuf, PositionSource>
{
    public PositionSource Decode(RegistryFriendlyByteBuf buf)
    {
        var id = buf.ReadVarInt();
        var type = BuiltInRegistries.POSITION_SOURCE_TYPE.ById(id) as PositionSourceType
            ?? throw new InvalidOperationException($"unknown position source type id {id}");
        return type.ReadParameters(buf);
    }

    public void Encode(RegistryFriendlyByteBuf buf, PositionSource value)
    {
        var type = value.Type;
        var id = BuiltInRegistries.POSITION_SOURCE_TYPE.GetId(type);
        if (id < 0) throw new InvalidOperationException($"position source type not registered: {type}");
        buf.WriteVarInt(id);
        type.WriteParameters(buf, value);
    }
}
