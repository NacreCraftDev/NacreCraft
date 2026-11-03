using NacreCraft.Codec;
using NacreCraft.Game.Commands.Arguments;
using NacreCraft.Game.World.Level;
using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//PlayerPredicate the player specific part of an entity predicate, maps to vanilla
//net.minecraft.advancements.predicates.entity.PlayerPredicate
//Vanilla also reads food/stats/recipes/advancements/input; those subsystems are not wired up, so only the level, the
//game mode and the looked at entity are read and the remaining fields are ignored
public sealed record PlayerPredicate(MinMaxBounds.Ints Level, GameTypePredicate GameType,
    Optional<EntityPredicate> LookingAt) : EntitySubPredicate
{
    //Codec persistence codec, field names level/gamemode/looking_at, maps to vanilla CODEC
    public static readonly Codec<PlayerPredicate> Codec = RecordCodecBuilder.Of3(
        MinMaxBounds.Ints.CODEC.OptionalFieldOf("level", MinMaxBounds.Ints.Any)
            .ForGetter((PlayerPredicate predicate) => predicate.Level),
        GameTypePredicate.Codec.OptionalFieldOf("gamemode", GameTypePredicate.Any)
            .ForGetter((PlayerPredicate predicate) => predicate.GameType),
        EntityPredicate.Codec.OptionalFieldOf("looking_at")
            .ForGetter((PlayerPredicate predicate) => predicate.LookingAt),
        (level, gameType, lookingAt) => new PlayerPredicate(level, gameType, lookingAt));

    //Matches every field the predicate carries against the player, maps to vanilla matches
    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position)
    {
        if (entity is not NacreCraft.Game.World.Entity.Player player) return false;
        if (!Level.Matches(player.XpLevel)) return false;
        if (NacreCraft.Game.World.Level.GameType.ById(player.GameMode) is not { } gameType
            || !GameType.Matches(gameType)) return false;
        return !LookingAt.IsPresent || LookingAt.Get().Matches(level, position, player);
    }
}
