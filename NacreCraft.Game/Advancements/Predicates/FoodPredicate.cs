using NacreCraft.Codec;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Entity;

namespace NacreCraft.Game.Advancements.Predicates;

//FoodPredicate food state predicate, maps to vanilla net.minecraft.advancements.predicates.FoodPredicate
//Checks the hunger level and saturation; NC has no FoodData class, the player carries the level itself and saturation is not tracked
public sealed record FoodPredicate(MinMaxBounds.Ints Level, MinMaxBounds.Doubles Saturation)
{
    public static readonly FoodPredicate Any = new(MinMaxBounds.Ints.Any, MinMaxBounds.Doubles.Any);

    //Codec persistence codec, field names level/saturation, maps to vanilla CODEC
    public static readonly Codec<FoodPredicate> Codec = RecordCodecBuilder.Of2(
        MinMaxBounds.Ints.CODEC.OptionalFieldOf("level", MinMaxBounds.Ints.Any)
            .ForGetter((FoodPredicate predicate) => predicate.Level),
        MinMaxBounds.Doubles.CODEC.OptionalFieldOf("saturation", MinMaxBounds.Doubles.Any)
            .ForGetter((FoodPredicate predicate) => predicate.Saturation),
        (level, saturation) => new FoodPredicate(level, saturation));

    //Matches the hunger level and saturation, maps to vanilla matches
    public bool Matches(int foodLevel, double saturationLevel)
        => Level.Matches(foodLevel) && Saturation.Matches(saturationLevel);

    //Matches against a player's food state; saturation stays 0 because no saturation stat is tracked yet
    public bool Matches(Player player) => Matches(player.FoodLevel, 0d);

    public sealed class Builder
    {
        private MinMaxBounds.Ints _level = MinMaxBounds.Ints.Any;
        private MinMaxBounds.Doubles _saturation = MinMaxBounds.Doubles.Any;

        public static Builder Food() => new();

        public Builder WithLevel(MinMaxBounds.Ints level)
        {
            _level = level;
            return this;
        }

        public Builder WithSaturation(MinMaxBounds.Doubles saturation)
        {
            _saturation = saturation;
            return this;
        }

        public FoodPredicate Build() => new(_level, _saturation);
    }
}
