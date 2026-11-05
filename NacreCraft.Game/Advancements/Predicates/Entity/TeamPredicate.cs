using NacreCraft.Codec;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Advancements.Predicates.Entity;

//TeamPredicate entity team predicate, maps to vanilla net.minecraft.advancements.predicates.entity.TeamPredicate
//Vanilla compares the entity's scoreboard team name; the scoreboard subsystem is not ported, so no entity carries a team
public sealed record TeamPredicate(string Team) : EntitySubPredicate
{
    public static readonly Codec<TeamPredicate> Codec = Codecs.String.ComapFlatMap(
        name => DataResult<TeamPredicate>.Success(new TeamPredicate(name)),
        predicate => predicate.Team);

    public bool Matches(NacreCraft.Registry.Entity entity, ILevelReader? level, Vec3? position) => false;
}
