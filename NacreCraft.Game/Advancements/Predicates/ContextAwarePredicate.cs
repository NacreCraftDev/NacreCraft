using NacreCraft.Codec;
using NacreCraft.Game.World.Loot;
using LootItemCondition = NacreCraft.Game.World.Loot.LootItemCondition;

namespace NacreCraft.Game.Advancements.Predicates;

//ContextAwarePredicate loot context predicate, maps to vanilla net.minecraft.advancements.predicates.ContextAwarePredicate
//A condition list combined with AND; an empty list always matches
public sealed class ContextAwarePredicate
{
    //Codec condition list, maps to vanilla CODEC
    public static readonly Codec<ContextAwarePredicate> Codec =
        LootItemConditions.TYPED_CODEC.ListOf().ComapFlatMap(
            conditions => DataResult<ContextAwarePredicate>.Success(new ContextAwarePredicate(conditions)),
            predicate => predicate.Conditions);

    public ContextAwarePredicate(IReadOnlyList<LootItemCondition> conditions) => Conditions = conditions;

    public IReadOnlyList<LootItemCondition> Conditions { get; }

    public static ContextAwarePredicate Create(params LootItemCondition[] conditions)
        => new(conditions);

    //Matches all conditions must pass, maps to vanilla matches
    public bool Matches(LootContext context)
    {
        foreach (var condition in Conditions)
            if (!condition.Test(context))
                return false;
        return true;
    }
}
