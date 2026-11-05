using NacreCraft.Codec;

namespace NacreCraft.Game.World.Entity.Variant;

//SpawnPrioritySelectors the selector list of a spawn variant, maps to vanilla net.minecraft.world.entity.variant.SpawnPrioritySelectors
public sealed record SpawnPrioritySelectors(IReadOnlyList<PrioritySelector<SpawnContext>> Selectors)
{
    public static readonly SpawnPrioritySelectors EMPTY = new(Array.Empty<PrioritySelector<SpawnContext>>());

    public static readonly Codec<SpawnPrioritySelectors> CODEC = SelectorCodec().ListOf()
        .ComapFlatMap(list => DataResult<SpawnPrioritySelectors>.Success(new SpawnPrioritySelectors(list)),
            selectors => selectors.Selectors);

    public static SpawnPrioritySelectors Single(SpawnCondition condition, int priority)
        => new(PriorityProviders.Single(condition, priority));

    public static SpawnPrioritySelectors Fallback(int priority)
        => new(PriorityProviders.AlwaysTrue<SpawnContext>(priority));

    private static Codec<PrioritySelector<SpawnContext>> SelectorCodec()
        => RecordCodecBuilder.Of2(
            SpawnCondition.CODEC.OptionalFieldOf("condition")
                .ForGetter((PrioritySelector<SpawnContext> selector) => Optional<SpawnCondition>.OfNullable(selector.Condition as SpawnCondition)),
            Codecs.Int.FieldOf("priority").ForGetter((PrioritySelector<SpawnContext> selector) => selector.Priority),
            (condition, priority) => new PrioritySelector<SpawnContext>(
                condition.IsPresent ? condition.Get() : null, priority));
}
