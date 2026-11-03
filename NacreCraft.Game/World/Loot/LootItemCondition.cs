using NacreCraft.Codec;
using NacreCraft.Game.Advancements.Predicates;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Loot;

//LootItemCondition a predicate gating a pool, entry or function, maps to vanilla LootItemCondition
public interface LootItemCondition
{
    bool Test(LootContext context);

    //TypeId is the registry name the condition field dispatches on
    Identifier TypeId { get; }

    MapCodec<LootItemCondition> Codec();
}

//ExplosionCondition keeps a drop with probability 1/radius, the condition that makes blocks decay in an explosion
public sealed class ExplosionCondition : LootItemCondition
{
    public static readonly ExplosionCondition Instance = new();

    public static readonly MapCodec<LootItemCondition> MAP_CODEC = Codecs.Unit<LootItemCondition>(Instance);

    private ExplosionCondition() { }

    public Identifier TypeId => Identifier.WithDefaultNamespace("survives_explosion");

    public bool Test(LootContext context)
    {
        if (!context.HasParameter(LootContextParams.ExplosionRadius)) return true;
        var radius = context.GetParameter(LootContextParams.ExplosionRadius);
        return context.Random.NextFloat() <= 1f / radius;
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//MatchTool passes when the tool context parameter satisfies the item predicate, maps to vanilla MatchTool
//The predicate field is optional: absent means any tool at all matches
public sealed record MatchTool(Optional<ItemPredicate> Predicate) : LootItemCondition
{
    public static readonly MapCodec<LootItemCondition> MAP_CODEC =
        RecordCodecBuilder.Of1<LootItemCondition, Optional<ItemPredicate>>(
            ItemPredicate.Codec.OptionalFieldOf("predicate")
                .ForGetter<LootItemCondition, Optional<ItemPredicate>>(c => ((MatchTool)c).Predicate),
            predicate => new MatchTool(predicate));

    public Identifier TypeId => Identifier.WithDefaultNamespace("match_tool");

    public bool Test(LootContext context)
    {
        if (!context.HasParameter(LootContextParams.Tool)) return false;
        var tool = context.GetParameter(LootContextParams.Tool);
        return !Predicate.IsPresent || Predicate.Get().Test(tool);
    }

    public MapCodec<LootItemCondition> Codec() => MAP_CODEC;
}

//LootItemConditions the condition type registry plus the root codec, maps to vanilla LootItemConditions
public static class LootItemConditions
{
    private static readonly Dictionary<Identifier, MapCodec<LootItemCondition>> Types = new();

    //TYPED_CODEC dispatches on the condition field, the shape vanilla writes for a typed condition
    public static readonly Codec<LootItemCondition> TYPED_CODEC =
        IdentifierCodec.Instance.Dispatch<LootItemCondition, Identifier>("condition", c => c.TypeId, Lookup);

    static LootItemConditions()
    {
        //Registration order follows vanilla LootItemConditions.bootstrap
        Register("inverted", InvertedCondition.MAP_CODEC);
        Register("any_of", AnyOfCondition.MAP_CODEC);
        Register("all_of", AllOfCondition.MAP_CODEC);
        Register("random_chance", RandomChanceCondition.MAP_CODEC);
        Register("random_chance_with_enchanted_bonus", RandomChanceWithEnchantedBonusCondition.MAP_CODEC);
        Register("entity_properties", EntityPropertiesCondition.MAP_CODEC);
        Register("killed_by_player", KilledByPlayerCondition.MAP_CODEC);
        Register("block_state_property", BlockStatePropertyCondition.MAP_CODEC);
        Register("match_tool", MatchTool.MAP_CODEC);
        Register("table_bonus", TableBonusCondition.MAP_CODEC);
        Register("survives_explosion", ExplosionCondition.MAP_CODEC);
        Register("damage_source_properties", DamageSourcePropertiesCondition.MAP_CODEC);
        Register("location_check", LocationCheckCondition.MAP_CODEC);
        Register("weather_check", WeatherCheckCondition.MAP_CODEC);
        Register("enchantment_active_check", EnchantmentActiveCheckCondition.MAP_CODEC);
    }

    private static MapCodec<LootItemCondition> Lookup(Identifier id)
        => Types.TryGetValue(id, out var codec)
            ? codec
            : throw new KeyNotFoundException($"Unknown loot condition type: {id}");

    private static void Register(string name, MapCodec<LootItemCondition> codec)
        => Types[Identifier.WithDefaultNamespace(name)] = codec;
}
