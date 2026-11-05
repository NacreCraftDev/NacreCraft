using NacreCraft.Codec;
using NacreCraft.Util.Collection;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Entity.Variant;

//SelectorCondition a predicate over the variant context, maps to vanilla PriorityProvider.SelectorCondition
public interface SelectorCondition<in TContext>
{
    bool Test(TContext context);
}

//PrioritySelector one condition at a priority, maps to vanilla PriorityProvider.Selector
public sealed record PrioritySelector<TContext>(SelectorCondition<TContext>? Condition, int Priority)
{
    public static PrioritySelector<TContext> Of(SelectorCondition<TContext> condition, int priority)
        => new(condition, priority);

    public static PrioritySelector<TContext> AlwaysTrue(int priority) => new(null, priority);
}

//PriorityProvider a set of priority selectors, maps to vanilla net.minecraft.world.entity.variant.PriorityProvider
public interface PriorityProvider<TContext, TCondition> where TCondition : class, SelectorCondition<TContext>
{
    IReadOnlyList<PrioritySelector<TContext>> Selectors();
}

public static class PriorityProviders
{
    //Select unpacks every entry's selectors, sorts by priority and keeps the highest matching priority band
    //maps to vanilla select
    public static IReadOnlyList<T> Select<T, TContext, TCondition>(IEnumerable<T> entries,
        Func<T, PriorityProvider<TContext, TCondition>> extractor, TContext context)
        where TCondition : class, SelectorCondition<TContext>
    {
        var unpacked = new List<UnpackedEntry<TContext, T>>();
        foreach (var entry in entries)
            foreach (var selector in extractor(entry).Selectors())
                unpacked.Add(new UnpackedEntry<TContext, T>(entry, selector.Priority,
                    selector.Condition ?? new AlwaysTrueCondition<TContext>()));
        unpacked.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        var highestMatchedPriority = int.MinValue;
        var result = new List<T>();
        foreach (var entry in unpacked)
        {
            if (entry.Priority < highestMatchedPriority) continue;
            if (!entry.Condition.Test(context)) continue;
            highestMatchedPriority = entry.Priority;
            result.Add(entry.Entry);
        }
        return result;
    }

    //Pick selects the candidates and picks one at random, maps to vanilla pick
    public static Optional<T> Pick<T, TContext, TCondition>(IEnumerable<T> entries,
        Func<T, PriorityProvider<TContext, TCondition>> extractor, RandomSource randomSource, TContext context)
        where TCondition : class, SelectorCondition<TContext>
    {
        var selected = Select(entries, extractor, context);
        return selected.Count == 0
            ? Optional<T>.Empty()
            : Optional<T>.Of(RandomCollections.GetRandom(selected, randomSource));
    }

    public static IReadOnlyList<PrioritySelector<TContext>> Single<TContext>(SelectorCondition<TContext> condition, int priority)
        => new[] { new PrioritySelector<TContext>(condition, priority) };

    public static IReadOnlyList<PrioritySelector<TContext>> AlwaysTrue<TContext>(int priority)
        => new[] { new PrioritySelector<TContext>(null, priority) };

    public sealed record UnpackedEntry<TContext, T>(T Entry, int Priority, SelectorCondition<TContext> Condition);

    private sealed class AlwaysTrueCondition<TContext> : SelectorCondition<TContext>
    {
        public bool Test(TContext context) => true;
    }
}
