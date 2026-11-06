using NacreCraft.Codec;
using NacreCraft.Game.World.Entity;
using NacreCraft.Game.World.Inventory;
using NacreCraft.Game.World.Loot;
using NacreCraft.Registry;
using NacreCraft.Registry.Context;
using RegistrySlotSource = NacreCraft.Registry.SlotSource;

namespace NacreCraft.Game.World.Items.Slots;

//RangeSlotSource reads a named slot range from an entity or block entity in the loot context, maps to vanilla net.minecraft.world.item.slot.RangeSlotSource
//Vanilla's source field is LootContextArg.ENTITY_OR_BLOCK; this port spells the same names out in a table because the project has no LootContextArg layer
public sealed class RangeSlotSource : SlotSource
{
    //Canonical name to context key plus resolver, mirrors LootContext.EntityTarget names and block_entity
    private static readonly Dictionary<string, (ContextKey Key, Func<LootContext, object?> Resolve)> Sources = new(StringComparer.Ordinal)
    {
        ["this"] = (LootContextParams.ThisEntity, context => context.GetOptionalParameter(LootContextParams.ThisEntity)),
        ["attacker"] = (LootContextParams.AttackingEntity, context => context.GetOptionalParameter(LootContextParams.AttackingEntity)),
        ["direct_attacker"] = (LootContextParams.DirectAttackingEntity, context => context.GetOptionalParameter(LootContextParams.DirectAttackingEntity)),
        ["attacking_player"] = (LootContextParams.LastDamagePlayer, context => context.GetOptionalParameter(LootContextParams.LastDamagePlayer)),
        ["target_entity"] = (LootContextParams.TargetEntity, context => context.GetOptionalParameter(LootContextParams.TargetEntity)),
        ["interacting_entity"] = (LootContextParams.InteractingEntity, context => context.GetOptionalParameter(LootContextParams.InteractingEntity)),
        ["block_entity"] = (LootContextParams.BlockEntity, context => context.GetOptionalParameter(LootContextParams.BlockEntity)),
    };

    //SourceCodec rejects names outside the table, matching the vanilla dispatch codec
    private static readonly Codec<string> SourceCodec = Codecs.String.ComapFlatMap(
        name => Sources.ContainsKey(name)
            ? DataResult<string>.Success(name)
            : DataResult<string>.Error(() => $"Unknown loot context source: {name}"),
        name => name);

    public static readonly MapCodec<RangeSlotSource> MAP_CODEC = RecordCodecBuilder.Of2(
        SourceCodec.FieldOf("source").ForGetter((RangeSlotSource source) => source.Source),
        SlotRanges.Codec.FieldOf("slots").ForGetter((RangeSlotSource source) => source.SlotRange),
        (source, range) => new RangeSlotSource(source, range));

    public static readonly MapCodec<RegistrySlotSource> ControlCodec =
        new XmapMapCodec<RangeSlotSource, RegistrySlotSource>(MAP_CODEC, value => value,
            source => (RangeSlotSource)source);

    private RangeSlotSource(string source, SlotRange slotRange)
    {
        Source = source;
        SlotRange = slotRange;
    }

    public string Source { get; }

    public SlotRange SlotRange { get; }

    public MapCodec<RegistrySlotSource> Codec() => ControlCodec;

    public ContextKey GetReferencedContextParam() => Sources[Source].Key;

    public SlotCollection Provide(LootContext context)
        => Sources[Source].Resolve(context) is SlotProvider provider
            ? provider.GetSlotsFromRange(SlotRange.Slots)
            : SlotCollections.EMPTY;
}
