using NacreCraft.Codec;
using NacreCraft.Game.Advancements.Predicates;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Loot;

//SetCountFunction sets or adds to the stack count, maps to vanilla SetItemCountFunction
public sealed record SetCountFunction(NumberProvider Count, bool Add, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of3<LootItemFunction, NumberProvider, bool, IReadOnlyList<LootItemCondition>>(
            NumberProviders.CODEC.FieldOf("count")
                .ForGetter<LootItemFunction, NumberProvider>(f => ((SetCountFunction)f).Count),
            Codecs.Bool.OptionalFieldOf("add", false).ForGetter<LootItemFunction, bool>(f => ((SetCountFunction)f).Add),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (count, add, conditions) => new SetCountFunction(count, add, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_count");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        stack.SetCount((Add ? stack.GetCount() : 0) + Count.GetInt(context));
        return stack;
    }
}

//SetDamageFunction sets the durability ratio of the stack, maps to vanilla SetItemDamageFunction
public sealed record SetDamageFunction(NumberProvider Damage, bool Add, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of3<LootItemFunction, NumberProvider, bool, IReadOnlyList<LootItemCondition>>(
            NumberProviders.CODEC.FieldOf("damage")
                .ForGetter<LootItemFunction, NumberProvider>(f => ((SetDamageFunction)f).Damage),
            Codecs.Bool.OptionalFieldOf("add", false).ForGetter<LootItemFunction, bool>(f => ((SetDamageFunction)f).Add),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (damage, add, conditions) => new SetDamageFunction(damage, add, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_damage");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        var maxDamage = stack.Get<object>(DataComponents.MAX_DAMAGE) is int max ? max : 0;
        if (maxDamage <= 0) return stack;
        var current = stack.Get<object>(DataComponents.DAMAGE) is int damage ? damage : 0;
        var baseValue = Add ? 1f - (float)current / maxDamage : 0f;
        var ratio = 1f - Math.Clamp(Damage.GetFloat(context) + baseValue, 0f, 1f);
        stack.Set<object>(DataComponents.DAMAGE, (int)MathF.Floor(ratio * maxDamage));
        return stack;
    }
}

//LimitCountFunction clamps the stack count into a range, maps to vanilla LimitCount
public sealed record LimitCountFunction(IntRange Limit, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, IntRange, IReadOnlyList<LootItemCondition>>(
            IntRange.Codec.FieldOf("limit").ForGetter<LootItemFunction, IntRange>(f => ((LimitCountFunction)f).Limit),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (limit, conditions) => new LimitCountFunction(limit, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("limit_count");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        stack.SetCount(Limit.Clamp(context, stack.GetCount()));
        return stack;
    }
}

//ExplosionDecayFunction drops each unit with the survival chance of an explosion, maps to vanilla ApplyExplosionDecay
public sealed record ExplosionDecayFunction(IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemFunction, IReadOnlyList<LootItemCondition>>(
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            conditions => new ExplosionDecayFunction(conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("explosion_decay");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        if (!context.HasParameter(LootContextParams.ExplosionRadius)) return stack;
        var probability = 1f / context.GetParameter(LootContextParams.ExplosionRadius);
        var kept = 0;
        var count = stack.GetCount();
        for (var index = 0; index < count; index++)
            if (context.Random.NextFloat() <= probability)
                kept++;
        stack.SetCount(kept);
        return stack;
    }
}

//DiscardFunction empties the stack, maps to vanilla DiscardItem
public sealed record DiscardFunction(IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemFunction, IReadOnlyList<LootItemCondition>>(
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            conditions => new DiscardFunction(conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("discard");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context) => ItemStack.Empty;
}

//FilteredFunction picks the pass or fail function by the item filter, maps to vanilla FilteredFunction
public sealed record FilteredFunction(ItemPredicate Filter, Optional<LootItemFunction> OnPass,
    Optional<LootItemFunction> OnFail, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of4<LootItemFunction, ItemPredicate, Optional<LootItemFunction>, Optional<LootItemFunction>,
            IReadOnlyList<LootItemCondition>>(
            ItemPredicate.Codec.FieldOf("item_filter")
                .ForGetter<LootItemFunction, ItemPredicate>(f => ((FilteredFunction)f).Filter),
            LootItemFunctions.TYPED_CODEC.OptionalFieldOf("on_pass")
                .ForGetter<LootItemFunction, Optional<LootItemFunction>>(f => ((FilteredFunction)f).OnPass),
            LootItemFunctions.TYPED_CODEC.OptionalFieldOf("on_fail")
                .ForGetter<LootItemFunction, Optional<LootItemFunction>>(f => ((FilteredFunction)f).OnFail),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (filter, onPass, onFail, conditions) => new FilteredFunction(filter, onPass, onFail, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("filtered");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        var chosen = Filter.Test(stack) ? OnPass : OnFail;
        return chosen.IsPresent ? chosen.Get().Apply(stack, context) : stack;
    }
}

//SetComponentsFunction merges a component patch into the stack, maps to vanilla SetComponentsFunction
public sealed record SetComponentsFunction(DataComponentPatch Components,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, DataComponentPatch, IReadOnlyList<LootItemCondition>>(
            DataComponentPatch.PersistentCodec.FieldOf("components")
                .ForGetter<LootItemFunction, DataComponentPatch>(f => ((SetComponentsFunction)f).Components),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (components, conditions) => new SetComponentsFunction(components, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_components");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the patch application path on the item component map
        return stack;
    }
}

//CopyComponentsFunction copies components from a context source, maps to vanilla CopyComponentsFunction
//source accepts an entity, block entity or item stack reference; its codec arrives with the loot context argument layer,
//so the field is decoded permissively and the behaviour waits
public sealed record CopyComponentsFunction(object Source, Optional<IReadOnlyList<Identifier>> Include,
    Optional<IReadOnlyList<Identifier>> Exclude, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of4<LootItemFunction, object, Optional<IReadOnlyList<Identifier>>,
            Optional<IReadOnlyList<Identifier>>, IReadOnlyList<LootItemCondition>>(
            IgnoredValueCodec.Instance.FieldOf("source")
                .ForGetter<LootItemFunction, object>(f => ((CopyComponentsFunction)f).Source),
            IdentifierCodec.Instance.ListOf().OptionalFieldOf("include")
                .ForGetter<LootItemFunction, Optional<IReadOnlyList<Identifier>>>(f => ((CopyComponentsFunction)f).Include),
            IdentifierCodec.Instance.ListOf().OptionalFieldOf("exclude")
                .ForGetter<LootItemFunction, Optional<IReadOnlyList<Identifier>>>(f => ((CopyComponentsFunction)f).Exclude),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (source, include, exclude, conditions) => new CopyComponentsFunction(source, include, exclude, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("copy_components");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the block entity component map and the loot context argument layer
        return stack;
    }
}

//CopyStateFunction copies block state properties onto the stack, maps to vanilla CopyBlockState
public sealed record CopyStateFunction(Holder<Block> Block, IReadOnlyList<string> Properties,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of3<LootItemFunction, Holder<Block>, IReadOnlyList<string>,
            IReadOnlyList<LootItemCondition>>(
            HolderSetCodecs.BlockRef.FieldOf("block")
                .ForGetter<LootItemFunction, Holder<Block>>(f => ((CopyStateFunction)f).Block),
            Codecs.String.ListOf().FieldOf("properties")
                .ForGetter<LootItemFunction, IReadOnlyList<string>>(f => ((CopyStateFunction)f).Properties),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (block, properties, conditions) => new CopyStateFunction(block, properties, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("copy_state");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the block state component, which arrives with the item component layer
        return stack;
    }
}

//NameTarget which display name the naming function writes, maps to vanilla SetNameFunction.Target
public enum NameTarget
{
    CustomName,
    ItemName,
}

//NameTargets name codec for the naming target, maps to vanilla Target.CODEC
public static class NameTargets
{
    public static readonly Codec<NameTarget> Codec = Codecs.String.ComapFlatMap(
        name => name switch
        {
            "custom_name" => DataResult<NameTarget>.Success(NameTarget.CustomName),
            "item_name" => DataResult<NameTarget>.Success(NameTarget.ItemName),
            _ => DataResult<NameTarget>.Error(() => $"Unknown name target {name}"),
        },
        GetSerializedName);

    public static string GetSerializedName(NameTarget target)
        => target == NameTarget.ItemName ? "item_name" : "custom_name";
}

//SetNameFunction writes a display name onto the stack, maps to vanilla SetNameFunction
public sealed record SetNameFunction(Optional<NacreCraft.Network.Chat.Component> Name,
    Optional<LootContext.EntityTarget> Entity, NameTarget Target, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of4<LootItemFunction, Optional<NacreCraft.Network.Chat.Component>,
            Optional<LootContext.EntityTarget>, NameTarget, IReadOnlyList<LootItemCondition>>(
            NacreCraft.Network.Chat.ComponentSerialization.JsonCodec.OptionalFieldOf("name")
                .ForGetter<LootItemFunction, Optional<NacreCraft.Network.Chat.Component>>(
                    f => ((SetNameFunction)f).Name),
            LootContext.EntityTargets.Codec.OptionalFieldOf("entity")
                .ForGetter<LootItemFunction, Optional<LootContext.EntityTarget>>(f => ((SetNameFunction)f).Entity),
            NameTargets.Codec.OptionalFieldOf("target", NameTarget.CustomName)
                .ForGetter<LootItemFunction, NameTarget>(f => ((SetNameFunction)f).Target),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (name, entity, target, conditions) => new SetNameFunction(name, entity, target, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_name");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the custom name and item name components, which arrive with the item component layer
        return stack;
    }
}

//ExplorationMapFunction turns the stack into a filled map pointing at a structure, maps to vanilla ExplorationMapFunction
//The decoration is decoded as an id because the map decoration registry is empty
public sealed record ExplorationMapFunction(TagKey<Structure> Destination, Identifier Decoration, byte Zoom,
    int SearchRadius, bool SkipExistingChunks, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    //The defaults must be declared before MAP_CODEC, static fields initialize in declaration order
    private static readonly TagKey<Structure> DefaultDestination =
        TagKey<Structure>.Create(Registries.STRUCTURE, Identifier.WithDefaultNamespace("on_treasure_maps"));

    private static readonly Identifier DefaultDecoration = Identifier.WithDefaultNamespace("mansion");

    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of6<LootItemFunction, TagKey<Structure>, Identifier, byte, int, bool,
            IReadOnlyList<LootItemCondition>>(
            TagKey<Structure>.Codec(Registries.STRUCTURE).OptionalFieldOf("destination", DefaultDestination)
                .ForGetter<LootItemFunction, TagKey<Structure>>(f => ((ExplorationMapFunction)f).Destination),
            IdentifierCodec.Instance.OptionalFieldOf("decoration", DefaultDecoration)
                .ForGetter<LootItemFunction, Identifier>(f => ((ExplorationMapFunction)f).Decoration),
            Codecs.Byte.OptionalFieldOf("zoom", (byte)2).ForGetter<LootItemFunction, byte>(f => ((ExplorationMapFunction)f).Zoom),
            Codecs.Int.OptionalFieldOf("search_radius", 50)
                .ForGetter<LootItemFunction, int>(f => ((ExplorationMapFunction)f).SearchRadius),
            Codecs.Bool.OptionalFieldOf("skip_existing_chunks", true)
                .ForGetter<LootItemFunction, bool>(f => ((ExplorationMapFunction)f).SkipExistingChunks),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (destination, decoration, zoom, searchRadius, skipExistingChunks, conditions) =>
                new ExplorationMapFunction(destination, decoration, zoom, searchRadius, skipExistingChunks, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("exploration_map");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the structure locator and the filled map component
        return stack;
    }
}

//StewEffectEntry one suspicious stew effect, maps to vanilla SetStewEffectFunction.EffectEntry
public sealed record StewEffectEntry(Holder<NacreCraft.Registry.MobEffect> Type, NumberProvider Duration)
{
    public static readonly Codec<StewEffectEntry> Codec = RecordCodecBuilder.Of2(
        HolderSetCodecs.MobEffectRef.FieldOf("type")
            .ForGetter((StewEffectEntry entry) => entry.Type),
        NumberProviders.CODEC.FieldOf("duration")
            .ForGetter((StewEffectEntry entry) => entry.Duration),
        (type, duration) => new StewEffectEntry(type, duration));
}

//SetStewEffectFunction writes suspicious stew effects onto the stack, maps to vanilla SetStewEffectFunction
public sealed record SetStewEffectFunction(IReadOnlyList<StewEffectEntry> Effects,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, IReadOnlyList<StewEffectEntry>, IReadOnlyList<LootItemCondition>>(
            StewEffectEntry.Codec.ListOf().OptionalFieldOf("effects", Array.Empty<StewEffectEntry>())
                .ForGetter<LootItemFunction, IReadOnlyList<StewEffectEntry>>(f => ((SetStewEffectFunction)f).Effects),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (effects, conditions) => new SetStewEffectFunction(effects, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_stew_effect");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the suspicious stew effects component, which arrives with the item component layer
        return stack;
    }
}

//SetRandomDyesFunction randomises the colour of a dyed item, maps to vanilla SetRandomDyesFunction
//The dye item tag lookup arrives with the tag layer
public sealed record SetRandomDyesFunction(NumberProvider NumberOfDyes,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, NumberProvider, IReadOnlyList<LootItemCondition>>(
            NumberProviders.CODEC.FieldOf("number_of_dyes")
                .ForGetter<LootItemFunction, NumberProvider>(f => ((SetRandomDyesFunction)f).NumberOfDyes),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (numberOfDyes, conditions) => new SetRandomDyesFunction(numberOfDyes, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_random_dyes");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the dye item tag and the dyed colour component
        return stack;
    }
}

//SetPotionFunction writes a fixed potion onto the stack, maps to vanilla SetPotionFunction
public sealed record SetPotionFunction(Holder<Potion> Potion, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, Holder<Potion>, IReadOnlyList<LootItemCondition>>(
            HolderSetCodecs.PotionRef.FieldOf("id")
                .ForGetter<LootItemFunction, Holder<Potion>>(f => ((SetPotionFunction)f).Potion),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (potion, conditions) => new SetPotionFunction(potion, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_potion");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        var contents = new PotionContents(Optional<Holder<NacreCraft.Registry.Potion>>.Of(Potion),
            Optional<int>.Empty(), Array.Empty<NacreCraft.Game.World.Effect.MobEffectInstance>(),
            Optional<string>.Empty());
        stack.Set<object>(DataComponents.POTION_CONTENTS, contents);
        return stack;
    }
}

//SetRandomPotionFunction writes a potion picked from the options onto the stack, maps to vanilla SetRandomPotionFunction
public sealed record SetRandomPotionFunction(Optional<HolderSet<Potion>> Options,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, Optional<HolderSet<Potion>>, IReadOnlyList<LootItemCondition>>(
            HolderSetCodecs.PotionSet.OptionalFieldOf("options")
                .ForGetter<LootItemFunction, Optional<HolderSet<Potion>>>(f => ((SetRandomPotionFunction)f).Options),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (options, conditions) => new SetRandomPotionFunction(options, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_random_potion");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        if (!Options.IsPresent || Options.Get().Size == 0) return stack;
        var options = Options.Get();
        var potion = options.Get(context.Random.NextInt(options.Size));
        var contents = new PotionContents(Optional<Holder<NacreCraft.Registry.Potion>>.Of(potion),
            Optional<int>.Empty(), Array.Empty<NacreCraft.Game.World.Effect.MobEffectInstance>(),
            Optional<string>.Empty());
        stack.Set<object>(DataComponents.POTION_CONTENTS, contents);
        return stack;
    }
}

//SetOminousBottleAmplifierFunction writes the ominous bottle amplifier, maps to vanilla
//SetOminousBottleAmplifierFunction
public sealed record SetOminousBottleAmplifierFunction(NumberProvider Amplifier,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, NumberProvider, IReadOnlyList<LootItemCondition>>(
            NumberProviders.CODEC.FieldOf("amplifier")
                .ForGetter<LootItemFunction, NumberProvider>(f => ((SetOminousBottleAmplifierFunction)f).Amplifier),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (amplifier, conditions) => new SetOminousBottleAmplifierFunction(amplifier, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_ominous_bottle_amplifier");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the ominous bottle amplifier component, which arrives with the item component layer
        return stack;
    }
}

//SetEnchantmentsFunction writes an enchantment map onto the stack, maps to vanilla SetEnchantmentsFunction
public sealed record SetEnchantmentsFunction(
    Dictionary<Holder<NacreCraft.Registry.Enchantment>, NumberProvider> Enchantments, bool Add,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of3<LootItemFunction,
            Dictionary<Holder<NacreCraft.Registry.Enchantment>, NumberProvider>, bool,
            IReadOnlyList<LootItemCondition>>(
            Codecs.UnboundedMap(HolderSetCodecs.EnchantmentRef, NumberProviders.CODEC)
                .OptionalFieldOf("enchantments",
                    new Dictionary<Holder<NacreCraft.Registry.Enchantment>, NumberProvider>())
                .ForGetter<LootItemFunction,
                    Dictionary<Holder<NacreCraft.Registry.Enchantment>, NumberProvider>>(
                    f => ((SetEnchantmentsFunction)f).Enchantments),
            Codecs.Bool.OptionalFieldOf("add", false)
                .ForGetter<LootItemFunction, bool>(f => ((SetEnchantmentsFunction)f).Add),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (enchantments, add, conditions) => new SetEnchantmentsFunction(enchantments, add, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_enchantments");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the enchantments component and the level based value path
        return stack;
    }
}

//EnchantRandomlyFunction enchants the stack with a random enchantment, maps to vanilla EnchantRandomlyFunction
public sealed record EnchantRandomlyFunction(Optional<HolderSet<NacreCraft.Registry.Enchantment>> Options,
    bool OnlyCompatible, bool IncludeAdditionalCostComponent, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of4<LootItemFunction, Optional<HolderSet<NacreCraft.Registry.Enchantment>>, bool, bool,
            IReadOnlyList<LootItemCondition>>(
            HolderSetCodecs.EnchantmentSet.OptionalFieldOf("options")
                .ForGetter<LootItemFunction, Optional<HolderSet<NacreCraft.Registry.Enchantment>>>(
                    f => ((EnchantRandomlyFunction)f).Options),
            Codecs.Bool.OptionalFieldOf("only_compatible", true)
                .ForGetter<LootItemFunction, bool>(f => ((EnchantRandomlyFunction)f).OnlyCompatible),
            Codecs.Bool.OptionalFieldOf("include_additional_cost_component", false)
                .ForGetter<LootItemFunction, bool>(f => ((EnchantRandomlyFunction)f).IncludeAdditionalCostComponent),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (options, onlyCompatible, includeAdditionalCostComponent, conditions) =>
                new EnchantRandomlyFunction(options, onlyCompatible, includeAdditionalCostComponent, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("enchant_randomly");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the enchantment registry and the enchantments component
        return stack;
    }
}

//EnchantWithLevelsFunction enchants the stack with a number of levels, maps to vanilla EnchantWithLevelsFunction
public sealed record EnchantWithLevelsFunction(NumberProvider Levels,
    Optional<HolderSet<NacreCraft.Registry.Enchantment>> Options, bool IncludeAdditionalCostComponent,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of4<LootItemFunction, NumberProvider,
            Optional<HolderSet<NacreCraft.Registry.Enchantment>>, bool, IReadOnlyList<LootItemCondition>>(
            NumberProviders.CODEC.FieldOf("levels")
                .ForGetter<LootItemFunction, NumberProvider>(f => ((EnchantWithLevelsFunction)f).Levels),
            HolderSetCodecs.EnchantmentSet.OptionalFieldOf("options")
                .ForGetter<LootItemFunction, Optional<HolderSet<NacreCraft.Registry.Enchantment>>>(
                    f => ((EnchantWithLevelsFunction)f).Options),
            Codecs.Bool.OptionalFieldOf("include_additional_cost_component", false)
                .ForGetter<LootItemFunction, bool>(f => ((EnchantWithLevelsFunction)f).IncludeAdditionalCostComponent),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (levels, options, includeAdditionalCostComponent, conditions) =>
                new EnchantWithLevelsFunction(levels, options, includeAdditionalCostComponent, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("enchant_with_levels");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the enchantment registry and the enchantments component
        return stack;
    }
}

//EnchantedCountIncreaseFunction adds to the count by the level of an enchantment, maps to vanilla
//EnchantedCountIncreaseFunction
//The enchantment level lookup arrives with the enchantment layer, until then the base count applies
public sealed record EnchantedCountIncreaseFunction(Holder<NacreCraft.Registry.Enchantment> Enchantment,
    NumberProvider Count, int Limit, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of4<LootItemFunction, Holder<NacreCraft.Registry.Enchantment>, NumberProvider, int,
            IReadOnlyList<LootItemCondition>>(
            HolderSetCodecs.EnchantmentRef.FieldOf("enchantment")
                .ForGetter<LootItemFunction, Holder<NacreCraft.Registry.Enchantment>>(
                    f => ((EnchantedCountIncreaseFunction)f).Enchantment),
            NumberProviders.CODEC.FieldOf("count")
                .ForGetter<LootItemFunction, NumberProvider>(f => ((EnchantedCountIncreaseFunction)f).Count),
            Codecs.Int.OptionalFieldOf("limit", 0)
                .ForGetter<LootItemFunction, int>(f => ((EnchantedCountIncreaseFunction)f).Limit),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (enchantment, count, limit, conditions) =>
                new EnchantedCountIncreaseFunction(enchantment, count, limit, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("enchanted_count_increase");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        stack.SetCount(stack.GetCount() + Count.GetInt(context));
        return stack;
    }
}

//FurnaceSmeltFunction replaces the stack with its smelting result, maps to vanilla SmeltItemFunction
public sealed record FurnaceSmeltFunction(bool UseInputCount, IReadOnlyList<LootItemCondition> Conditions)
    : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, bool, IReadOnlyList<LootItemCondition>>(
            Codecs.Bool.OptionalFieldOf("use_input_count", true)
                .ForGetter<LootItemFunction, bool>(f => ((FurnaceSmeltFunction)f).UseInputCount),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (useInputCount, conditions) => new FurnaceSmeltFunction(useInputCount, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("furnace_smelt");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the smelting recipe lookup
        return stack;
    }
}

//SetInstrumentFunction writes a goat horn instrument onto the stack, maps to vanilla SetInstrumentFunction
//The instrument registry does not exist yet, so the options stay as raw ids
public sealed record SetInstrumentFunction(IReadOnlyList<string> Options,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemFunction, IReadOnlyList<string>, IReadOnlyList<LootItemCondition>>(
            StringListCodec.Instance.FieldOf("options")
                .ForGetter<LootItemFunction, IReadOnlyList<string>>(f => ((SetInstrumentFunction)f).Options),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (options, conditions) => new SetInstrumentFunction(options, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("set_instrument");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the instrument registry and the instrument component
        return stack;
    }
}

//BonusFormula one of the apply_bonus formulas, maps to vanilla ApplyBonusCount.Formula
public abstract record BonusFormula;

//UniformBonusCount adds a uniform multiplier of the level, maps to vanilla UniformBonusCount
public sealed record UniformBonusCount(int BonusMultiplier) : BonusFormula
{
    public static readonly MapCodec<BonusFormula> MAP_CODEC =
        RecordCodecBuilder.Of1<BonusFormula, int>(
            Codecs.Int.FieldOf("bonusMultiplier")
                .ForGetter<BonusFormula, int>(f => ((UniformBonusCount)f).BonusMultiplier),
            multiplier => new UniformBonusCount(multiplier));
}

//OreDrops multiplies the count by a level based factor, maps to vanilla OreDrops
public sealed record OreDrops : BonusFormula
{
    public static readonly OreDrops Instance = new();

    public static readonly MapCodec<BonusFormula> MAP_CODEC = Codecs.Unit<BonusFormula>(Instance);
}

//BinomialWithBonusCount draws extra rounds, maps to vanilla BinomialWithBonusCount
public sealed record BinomialWithBonusCount(int ExtraRounds, float Probability) : BonusFormula
{
    public static readonly MapCodec<BonusFormula> MAP_CODEC =
        RecordCodecBuilder.Of2<BonusFormula, int, float>(
            Codecs.Int.FieldOf("extra")
                .ForGetter<BonusFormula, int>(f => ((BinomialWithBonusCount)f).ExtraRounds),
            Codecs.Float.FieldOf("probability")
                .ForGetter<BonusFormula, float>(f => ((BinomialWithBonusCount)f).Probability),
            (extraRounds, probability) => new BinomialWithBonusCount(extraRounds, probability));
}

//BonusFormulaCodec dispatches on the formula field, maps to vanilla ApplyBonusCount.FORMULA_CODEC
//The parameters live in a nested object, an absent formula means the default uniform bonus count
public sealed class BonusFormulaCodec : AbstractMapCodec<BonusFormula>
{
    public static readonly BonusFormulaCodec Instance = new();

    public override DataResult<BonusFormula> Decode<U>(DynamicOps<U> ops, MapLike<U> input)
    {
        var formulaTag = input.Get("formula");
        var codec = UniformBonusCount.MAP_CODEC;
        if (formulaTag.IsPresent)
        {
            var formulaName = ops.GetStringValue(formulaTag.Get());
            if (!formulaName.Result().IsPresent)
                return DataResult<BonusFormula>.Error(() => "bonus formula name must be a string");
            var id = Identifier.TryParse(formulaName.GetOrThrow());
            if (id is null)
                return DataResult<BonusFormula>.Error(() => $"invalid bonus formula {formulaName.GetOrThrow()}");
            switch (id.Value.Path)
            {
                case "uniform_bonus_count":
                    codec = UniformBonusCount.MAP_CODEC;
                    break;
                case "ore_drops":
                    codec = OreDrops.MAP_CODEC;
                    break;
                case "binomial_with_bonus_count":
                    codec = BinomialWithBonusCount.MAP_CODEC;
                    break;
                default:
                    return DataResult<BonusFormula>.Error(() => $"unknown bonus formula {id}");
            }
        }

        var parametersTag = input.Get("parameters");
        if (!parametersTag.IsPresent)
            return ops.GetMap(ops.EmptyMap()).FlatMap(empty => codec.Decode(ops, empty));
        return ops.GetMap(parametersTag.Get()).FlatMap(parameters => codec.Decode(ops, parameters));
    }

    public override RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, BonusFormula value, RecordBuilder<U> builder)
        => throw new NotSupportedException("encoding bonus formulas is not supported yet");
}

//ApplyBonusFunction scales the count by an enchantment formula, maps to vanilla ApplyBonusCount
//The enchantment level lookup arrives with the enchantment layer
public sealed record ApplyBonusFunction(Holder<NacreCraft.Registry.Enchantment> Enchantment, BonusFormula Formula,
    IReadOnlyList<LootItemCondition> Conditions) : LootItemConditionalFunction(Conditions)
{
    public static readonly MapCodec<LootItemFunction> MAP_CODEC =
        RecordCodecBuilder.Of3<LootItemFunction, Holder<NacreCraft.Registry.Enchantment>, BonusFormula,
            IReadOnlyList<LootItemCondition>>(
            HolderSetCodecs.EnchantmentRef.FieldOf("enchantment")
                .ForGetter<LootItemFunction, Holder<NacreCraft.Registry.Enchantment>>(
                    f => ((ApplyBonusFunction)f).Enchantment),
            BonusFormulaCodec.Instance.ForGetter<LootItemFunction, BonusFormula>(f => ((ApplyBonusFunction)f).Formula),
            LootItemConditions.TYPED_CODEC.ListOf().OptionalFieldOf("conditions", Array.Empty<LootItemCondition>())
                .ForGetter<LootItemFunction, IReadOnlyList<LootItemCondition>>(f => f.Conditions),
            (enchantment, formula, conditions) => new ApplyBonusFunction(enchantment, formula, conditions));

    public override Identifier TypeId => Identifier.WithDefaultNamespace("apply_bonus");

    public override MapCodec<LootItemFunction> Codec() => MAP_CODEC;

    protected override ItemStack Run(ItemStack stack, LootContext context)
    {
        //Needs the enchantment level lookup
        return stack;
    }
}

//IgnoredValueCodec accepts any element and yields a placeholder, used by fields whose type lives in a layer that is not
//built yet
internal sealed class IgnoredValueCodec : ScalarCodec<object>
{
    public static readonly IgnoredValueCodec Instance = new();

    public override DataResult<object> Parse<U>(DynamicOps<U> ops, U input) => DataResult<object>.Success(new object());

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, object value)
        => DataResult<U>.Success(ops.Empty());
}

//StringListCodec accepts a single string or a list of strings, the shape vanilla writes for a list of ids
internal sealed class StringListCodec : ScalarCodec<IReadOnlyList<string>>
{
    public static readonly StringListCodec Instance = new();

    public override DataResult<IReadOnlyList<string>> Parse<U>(DynamicOps<U> ops, U input)
    {
        var single = ops.GetStringValue(input);
        if (single.Result().IsPresent)
            return DataResult<IReadOnlyList<string>>.Success(new[] { single.GetOrThrow() });
        return Codecs.String.ListOf().Parse(ops, input);
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, IReadOnlyList<string> value)
        => DataResult<U>.Error(() => "string lists are not encodable");
}
