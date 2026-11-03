using NacreCraft.Codec;
using NacreCraft.Game.Advancements.Predicates;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using EntityPredicate = NacreCraft.Game.Advancements.Predicates.Entity.EntityPredicate;

namespace NacreCraft.Game.World.Loot;

//AllOfCondition every term must pass, maps to vanilla AllOfCondition
public sealed record AllOfCondition(IReadOnlyList<LootItemCondition> Terms) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, IReadOnlyList<LootItemCondition>>(
            LootItemConditions.TYPED_CODEC.ListOf().FieldOf("terms")
                .ForGetter<LootItemCondition, IReadOnlyList<LootItemCondition>>(c => ((AllOfCondition)c).Terms),
            terms => new AllOfCondition(terms));

    public Identifier TypeId => Identifier.WithDefaultNamespace("all_of");

    public bool Test(LootContext context) => Terms.All(term => term.Test(context));

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//AnyOfCondition at least one term must pass, maps to vanilla AnyOfCondition
public sealed record AnyOfCondition(IReadOnlyList<LootItemCondition> Terms) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, IReadOnlyList<LootItemCondition>>(
            LootItemConditions.TYPED_CODEC.ListOf().FieldOf("terms")
                .ForGetter<LootItemCondition, IReadOnlyList<LootItemCondition>>(c => ((AnyOfCondition)c).Terms),
            terms => new AnyOfCondition(terms));

    public Identifier TypeId => Identifier.WithDefaultNamespace("any_of");

    public bool Test(LootContext context) => Terms.Any(term => term.Test(context));

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//InvertedCondition the term must fail, maps to vanilla InvertedLootItemCondition
public sealed record InvertedCondition(LootItemCondition Term) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, LootItemCondition>(
            LootItemConditions.TYPED_CODEC.FieldOf("term")
                .ForGetter<LootItemCondition, LootItemCondition>(c => ((InvertedCondition)c).Term),
            term => new InvertedCondition(term));

    public Identifier TypeId => Identifier.WithDefaultNamespace("inverted");

    public bool Test(LootContext context) => !Term.Test(context);

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//RandomChanceCondition passes with the probability the number provider draws, maps to vanilla LootItemRandomChanceCondition
public sealed record RandomChanceCondition(NumberProvider Chance) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, NumberProvider>(
            NumberProviders.CODEC.FieldOf("chance")
                .ForGetter<LootItemCondition, NumberProvider>(c => ((RandomChanceCondition)c).Chance),
            chance => new RandomChanceCondition(chance));

    public Identifier TypeId => Identifier.WithDefaultNamespace("random_chance");

    public bool Test(LootContext context) => context.Random.NextFloat() < Chance.GetFloat(context);

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//EntityPropertiesCondition the entity the target names must satisfy the predicate, maps to vanilla
//LootItemEntityPropertyCondition
public sealed record EntityPropertiesCondition(Optional<EntityPredicate> Predicate, LootContext.EntityTarget Target)
    : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemCondition, Optional<EntityPredicate>, LootContext.EntityTarget>(
            EntityPredicate.Codec.OptionalFieldOf("predicate")
                .ForGetter<LootItemCondition, Optional<EntityPredicate>>(
                    c => ((EntityPropertiesCondition)c).Predicate),
            LootContext.EntityTargets.Codec.FieldOf("entity")
                .ForGetter<LootItemCondition, LootContext.EntityTarget>(c => ((EntityPropertiesCondition)c).Target),
            (predicate, target) => new EntityPropertiesCondition(predicate, target));

    public Identifier TypeId => Identifier.WithDefaultNamespace("entity_properties");

    public bool Test(LootContext context)
    {
        var entity = LootContext.EntityTargets.GetEntity(Target, context);
        if (!Predicate.IsPresent) return true;
        return Predicate.Get().Matches(context.Level, context.GetOptionalParameter(LootContextParams.Origin), entity);
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//LocationCheckCondition the origin shifted by the offset must satisfy the predicate, maps to vanilla LocationCheck
public sealed record LocationCheckCondition(Optional<LocationPredicate> Predicate, BlockPos Offset) : LootItemCondition
{
    //OffsetCodec the three offset components sit directly in the condition object, maps to vanilla OFFSET_CODEC
    private static readonly Codec<BlockPos> OffsetCodec =
        RecordCodecBuilder.Of3<BlockPos, int, int, int>(
            Codecs.Int.OptionalFieldOf("offsetX", 0).ForGetter<BlockPos, int>(pos => pos.X),
            Codecs.Int.OptionalFieldOf("offsetY", 0).ForGetter<BlockPos, int>(pos => pos.Y),
            Codecs.Int.OptionalFieldOf("offsetZ", 0).ForGetter<BlockPos, int>(pos => pos.Z),
            (x, y, z) => new BlockPos(x, y, z));

    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemCondition, Optional<LocationPredicate>, BlockPos>(
            LocationPredicate.Codec.OptionalFieldOf("predicate")
                .ForGetter<LootItemCondition, Optional<LocationPredicate>>(
                    c => ((LocationCheckCondition)c).Predicate),
            OffsetCodec.ForGetter<LootItemCondition, BlockPos>(c => ((LocationCheckCondition)c).Offset),
            (predicate, offset) => new LocationCheckCondition(predicate, offset));

    public Identifier TypeId => Identifier.WithDefaultNamespace("location_check");

    public bool Test(LootContext context)
    {
        if (!context.HasParameter(LootContextParams.Origin)) return false;
        var pos = context.GetParameter(LootContextParams.Origin);
        return !Predicate.IsPresent || Predicate.Get().Matches(context.Level, pos.X + Offset.X,
            pos.Y + Offset.Y, pos.Z + Offset.Z);
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//DamageSourcePropertiesCondition the damage source must satisfy the predicate, maps to vanilla DamageSourceCondition
public sealed record DamageSourcePropertiesCondition(Optional<DamageSourcePredicate> Predicate) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, Optional<DamageSourcePredicate>>(
            DamageSourcePredicate.Codec.OptionalFieldOf("predicate")
                .ForGetter<LootItemCondition, Optional<DamageSourcePredicate>>(
                    c => ((DamageSourcePropertiesCondition)c).Predicate),
            predicate => new DamageSourcePropertiesCondition(predicate));

    public Identifier TypeId => Identifier.WithDefaultNamespace("damage_source_properties");

    public bool Test(LootContext context)
    {
        if (!context.HasParameter(LootContextParams.Origin)
            || !context.HasParameter(LootContextParams.DamageSource)) return false;
        var source = context.GetParameter(LootContextParams.DamageSource);
        var pos = context.GetParameter(LootContextParams.Origin);
        return !Predicate.IsPresent || Predicate.Get().Matches(context.Level, pos, source);
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//WeatherCheckCondition the level weather must match the present expectations, maps to vanilla WeatherCheck
public sealed record WeatherCheckCondition(Optional<bool> Raining, Optional<bool> Thundering) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemCondition, Optional<bool>, Optional<bool>>(
            Codecs.Bool.OptionalFieldOf("raining")
                .ForGetter<LootItemCondition, Optional<bool>>(c => ((WeatherCheckCondition)c).Raining),
            Codecs.Bool.OptionalFieldOf("thundering")
                .ForGetter<LootItemCondition, Optional<bool>>(c => ((WeatherCheckCondition)c).Thundering),
            (raining, thundering) => new WeatherCheckCondition(raining, thundering));

    public Identifier TypeId => Identifier.WithDefaultNamespace("weather_check");

    public bool Test(LootContext context)
    {
        if (Raining.IsPresent && Raining.Get() != context.Level.IsRaining) return false;
        return !Thundering.IsPresent || Thundering.Get() == context.Level.IsThundering;
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//BlockStatePropertyCondition the block state at the origin must be the named block and match the properties, maps to
//vanilla LootItemBlockStatePropertyCondition
public sealed record BlockStatePropertyCondition(Holder<Block> Block, Optional<StatePropertiesPredicate> Properties)
    : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemCondition, Holder<Block>, Optional<StatePropertiesPredicate>>(
            HolderSetCodecs.BlockRef.FieldOf("block")
                .ForGetter<LootItemCondition, Holder<Block>>(c => ((BlockStatePropertyCondition)c).Block),
            StatePropertiesPredicate.Codec.OptionalFieldOf("properties")
                .ForGetter<LootItemCondition, Optional<StatePropertiesPredicate>>(
                    c => ((BlockStatePropertyCondition)c).Properties),
            (block, properties) => new BlockStatePropertyCondition(block, properties));

    public Identifier TypeId => Identifier.WithDefaultNamespace("block_state_property");

    public bool Test(LootContext context)
    {
        if (!context.HasParameter(LootContextParams.BlockState)) return false;
        var state = context.GetParameter(LootContextParams.BlockState);
        return state.Is(Block.Value) && (!Properties.IsPresent || Properties.Get().Matches(state));
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//TableBonusCondition rolls against the chance at the enchantment level, maps to vanilla BonusLevelTableCondition
//The enchantment level lookup arrives with the enchantment layer, until then the level counts as zero
public sealed record TableBonusCondition(Holder<NacreCraft.Registry.Enchantment> Enchantment,
    IReadOnlyList<float> Values) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of2<LootItemCondition, Holder<NacreCraft.Registry.Enchantment>, IReadOnlyList<float>>(
            HolderSetCodecs.EnchantmentRef.FieldOf("enchantment")
                .ForGetter<LootItemCondition, Holder<NacreCraft.Registry.Enchantment>>(
                    c => ((TableBonusCondition)c).Enchantment),
            Codecs.Float.ListOf().FieldOf("chances")
                .ForGetter<LootItemCondition, IReadOnlyList<float>>(c => ((TableBonusCondition)c).Values),
            (enchantment, values) => new TableBonusCondition(enchantment, values));

    public Identifier TypeId => Identifier.WithDefaultNamespace("table_bonus");

    public bool Test(LootContext context)
    {
        if (Values.Count == 0) return false;
        return context.Random.NextFloat() < Values[0];
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//KilledByPlayerCondition the context must name the player that dealt the last damage, maps to vanilla
//LootItemKilledByPlayerCondition
public sealed record KilledByPlayerCondition : LootItemCondition
{
    public static readonly KilledByPlayerCondition Instance = new();

    public static readonly MapCodec<LootItemCondition> MAP_CODEC = Codecs.Unit<LootItemCondition>(Instance);

    private KilledByPlayerCondition()
    {
    }

    public Identifier TypeId => Identifier.WithDefaultNamespace("killed_by_player");

    public bool Test(LootContext context) => context.HasParameter(LootContextParams.LastDamagePlayer);

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//RandomChanceWithEnchantedBonusCondition rolls the enchanted chance at the enchantment level, maps to vanilla
//LootItemRandomChanceWithEnchantedBonusCondition
//The enchantment level lookup arrives with the enchantment layer, until then the unenchanted chance applies
public sealed record RandomChanceWithEnchantedBonusCondition(float UnenchantedChance,
    NacreCraft.Registry.LevelBasedValue EnchantedChance, Holder<NacreCraft.Registry.Enchantment> Enchantment)
    : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of3<LootItemCondition, float, NacreCraft.Registry.LevelBasedValue,
            Holder<NacreCraft.Registry.Enchantment>>(
            Codecs.Float.FieldOf("unenchanted_chance")
                .ForGetter<LootItemCondition, float>(c => ((RandomChanceWithEnchantedBonusCondition)c).UnenchantedChance),
            NacreCraft.Game.World.Items.Enchanting.LevelBasedValueCodec.Instance.FieldOf("enchanted_chance")
                .ForGetter<LootItemCondition, NacreCraft.Registry.LevelBasedValue>(
                    c => ((RandomChanceWithEnchantedBonusCondition)c).EnchantedChance),
            HolderSetCodecs.EnchantmentRef.FieldOf("enchantment")
                .ForGetter<LootItemCondition, Holder<NacreCraft.Registry.Enchantment>>(
                    c => ((RandomChanceWithEnchantedBonusCondition)c).Enchantment),
            (unenchantedChance, enchantedChance, enchantment) =>
                new RandomChanceWithEnchantedBonusCondition(unenchantedChance, enchantedChance, enchantment));

    public Identifier TypeId => Identifier.WithDefaultNamespace("random_chance_with_enchanted_bonus");

    public bool Test(LootContext context) => context.Random.NextFloat() < UnenchantedChance;

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//EnchantmentActiveCheckCondition the context must carry the expected enchantment active flag, maps to vanilla
//EnchantmentActiveCheck
public sealed record EnchantmentActiveCheckCondition(bool Active) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, bool>(
            Codecs.Bool.FieldOf("active")
                .ForGetter<LootItemCondition, bool>(c => ((EnchantmentActiveCheckCondition)c).Active),
            active => new EnchantmentActiveCheckCondition(active));

    public Identifier TypeId => Identifier.WithDefaultNamespace("enchantment_active_check");

    public bool Test(LootContext context) => context.GetParameter(LootContextParams.EnchantmentActive) == Active;

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}
