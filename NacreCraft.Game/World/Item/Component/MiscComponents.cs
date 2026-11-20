using NacreCraft.Codec;
using NacreCraft.Game.World.Damage;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Items.Component;

//CustomModelData extra float values passed to the item model, maps to vanilla net.minecraft.world.item.component.CustomModelData
public sealed record CustomModelData(IReadOnlyList<float> Floats)
{
    public static readonly CustomModelData Empty = new(Array.Empty<float>());

    public static readonly Codec<CustomModelData> Codec =
        Codecs.Float.ListOf().Xmap(values => new CustomModelData(values), value => value.Floats);

    public static readonly StreamCodec<FriendlyByteBuf, CustomModelData> StreamCodec =
        ByteBufCodecs.ListOf(ByteBufCodecs.Float)
            .Map(values => new CustomModelData(values), value => value.Floats.ToList());
}

//DamageResistant a set of damage types the holder ignores, maps to vanilla net.minecraft.world.item.component.DamageResistant
public sealed record DamageResistant(HolderSet<DamageType> Types)
{
    public static readonly Codec<DamageResistant> Codec =
        HolderSetCodecs.DamageTypeSet.Xmap(types => new DamageResistant(types), value => value.Types);

    public bool IsResistantTo(DamageSource source) => Types.Contains(source.TypeHolder);
}

//ProvidesTrimMaterial the trim material an item contributes, maps to vanilla net.minecraft.world.item.component.ProvidesTrimMaterial
public sealed record ProvidesTrimMaterial(Holder<TrimMaterial> Material)
{
    public static readonly Codec<ProvidesTrimMaterial> Codec =
        HolderSetCodecs.TrimMaterialRef.Xmap(material => new ProvidesTrimMaterial(material), value => value.Material);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, ProvidesTrimMaterial> StreamCodec =
        ByteBufCodecs.Holder(Registries.TRIM_MATERIAL).Map(material => new ProvidesTrimMaterial(material),
            value => value.Material);
}

//InstrumentComponent the instrument a goat horn plays, maps to vanilla net.minecraft.world.item.component.InstrumentComponent
public sealed record InstrumentComponent(Holder<Instrument> Instrument)
{
    public static readonly Codec<InstrumentComponent> Codec =
        HolderSetCodecs.InstrumentRef.Xmap(instrument => new InstrumentComponent(instrument), value => value.Instrument);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, InstrumentComponent> StreamCodec =
        ByteBufCodecs.Holder(Registries.INSTRUMENT).Map(instrument => new InstrumentComponent(instrument),
            value => value.Instrument);
}
