using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
using NacreCraft.Registry;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//SlotSources entry points for the slot sources, maps to vanilla net.minecraft.world.item.slot.SlotSources
public static class SlotSources
{
    //TYPED_CODEC dispatch by the slot_source_type registry, maps to vanilla TYPED_CODEC
    public static readonly Codec<SlotSource> TYPED_CODEC = new SlotSourceDispatchCodec();

    //CODEC the typed form with the inline group list as fallback, maps to vanilla CODEC
    public static readonly Codec<SlotSource> CODEC = Codecs.WithAlternative(TYPED_CODEC,
        GroupSlotSource.INLINE_CODEC.ComapFlatMap(
            group => DataResult<SlotSource>.Success(group),
            source => (GroupSlotSource)source));

    public static MapCodec<RegistrySlotSource> Bootstrap(Registry<MapCodec<RegistrySlotSource>> registry)
    {
        Registry<MapCodec<RegistrySlotSource>>.Register(registry,
            Identifier.WithDefaultNamespace("group"), GroupSlotSource.ControlCodec);
        Registry<MapCodec<RegistrySlotSource>>.Register(registry,
            Identifier.WithDefaultNamespace("filtered"), FilteredSlotSource.ControlCodec);
        Registry<MapCodec<RegistrySlotSource>>.Register(registry,
            Identifier.WithDefaultNamespace("limit_slots"), LimitSlotSource.ControlCodec);
        Registry<MapCodec<RegistrySlotSource>>.Register(registry,
            Identifier.WithDefaultNamespace("contents"), ContentsSlotSource.ControlCodec);
        return Registry<MapCodec<RegistrySlotSource>>.Register(registry,
            Identifier.WithDefaultNamespace("empty"), EmptySlotSource.ControlCodec);
    }

    //Group combines several sources into one provider, maps to vanilla group
    public static Func<LootContext, SlotCollection> Group(IReadOnlyCollection<SlotSource> terms)
    {
        var list = terms.ToList();
        return list.Count switch
        {
            0 => _ => SlotCollections.EMPTY,
            1 => list[0].Provide,
            2 => context => SlotCollections.Concat(list[0].Provide(context), list[1].Provide(context)),
            _ => context => SlotCollections.Concat(list.Select(term => term.Provide(context)).ToList())
        };
    }
}

//SlotSourceDispatchCodec dispatches on the type field through the slot_source_type registry, maps to vanilla TYPED_CODEC
internal sealed class SlotSourceDispatchCodec : ScalarCodec<SlotSource>
{
    public override DataResult<SlotSource> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<SlotSource>.Error(() => "slot source must be an object");
        var map = mapResult.Result().Get();
        var typeValue = map.Get("type");
        if (!typeValue.IsPresent) return DataResult<SlotSource>.Error(() => "slot source is missing the type field");
        var typeText = ops.GetStringValue(typeValue.Get()).Result();
        if (!typeText.IsPresent) return DataResult<SlotSource>.Error(() => "slot source type must be a string");
        var id = Identifier.TryParse(typeText.Get());
        if (id is null) return DataResult<SlotSource>.Error(() => $"Invalid slot source id: {typeText.Get()}");
        var codec = BuiltInRegistries.SLOT_SOURCE_TYPE.GetValue(id.Value);
        if (codec is null) return DataResult<SlotSource>.Error(() => $"Unknown slot source type: {id.Value}");
        return codec.Decode(ops, map).FlatMap(source => source is SlotSource typed
            ? DataResult<SlotSource>.Success(typed)
            : DataResult<SlotSource>.Error(() => $"Slot source {id.Value} is not a slot source"));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, SlotSource value)
    {
        var codec = value.Codec();
        var id = BuiltInRegistries.SLOT_SOURCE_TYPE.GetKey(codec);
        if (id is null) return DataResult<U>.Error(() => "unregistered slot source codec");
        return codec.EncodeStart(ops, value).FlatMap(body =>
            ops.MergeToMap(body, ops.CreateString("type"), ops.CreateString(id.ToString())));
    }
}
