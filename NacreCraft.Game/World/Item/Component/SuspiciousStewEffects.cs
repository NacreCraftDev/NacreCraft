using NacreCraft.Codec;
using NacreCraft.Game.World.Effect;
using NacreCraft.Network;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Items.Component;

//SuspiciousStewEffects the effects a suspicious stew grants, maps to vanilla net.minecraft.world.item.component.SuspiciousStewEffects
public sealed record SuspiciousStewEffects(IReadOnlyList<SuspiciousStewEffects.Entry> Effects)
{
    public const int DefaultDuration = 160;

    public static readonly SuspiciousStewEffects Empty = new(Array.Empty<Entry>());

    public static readonly Codec<SuspiciousStewEffects> Codec = Entry.Codec.ListOf()
        .Xmap(effects => new SuspiciousStewEffects(effects), value => value.Effects);

    public static readonly StreamCodec<RegistryFriendlyByteBuf, SuspiciousStewEffects> StreamCodec =
        ByteBufCodecs.ListOf(Entry.StreamCodec)
            .Map(effects => new SuspiciousStewEffects(effects), value => value.Effects.ToList());

    public SuspiciousStewEffects WithEffectAdded(Entry entry)
    {
        var effects = new List<Entry>(Effects) { entry };
        return new SuspiciousStewEffects(effects);
    }

    //Entry one effect and its duration, maps to vanilla SuspiciousStewEffects.Entry
    public sealed record Entry(Holder<NacreCraft.Registry.MobEffect> Effect, int Duration)
    {
        public static readonly Codec<Entry> Codec = RecordCodecBuilder.Of2(
            HolderSetCodecs.MobEffectRef.FieldOf("id").ForGetter((Entry value) => value.Effect),
            Codecs.Int.OptionalFieldOf("duration", DefaultDuration).ForGetter((Entry value) => value.Duration),
            (effect, duration) => new Entry(effect, duration));

        public static readonly StreamCodec<RegistryFriendlyByteBuf, Entry> StreamCodec = StreamCodecs.Composite(
            ByteBufCodecs.Holder(Registries.MOB_EFFECT), value => value.Effect,
            ByteBufCodecs.VarInt, value => value.Duration,
            (effect, duration) => new Entry(effect, duration));

        public MobEffectInstance CreateEffectInstance() => new(Effect, Duration, 0, false, true);
    }
}
