namespace NacreCraft.Game.Advancements.Predicates.Entity;

//EntitySubPredicate entity sub-predicate contract, maps to vanilla net.minecraft.advancements.predicates.entity.EntitySubPredicate
//The registry layer only declares the member shape, the game layer adds the always-true instance used by empty predicates
public interface EntitySubPredicate : NacreCraft.Registry.EntitySubPredicate
{
    //AlwaysTrue predicate that accepts every entity, maps to vanilla ALWAYS_TRUE
    public static EntitySubPredicate AlwaysTrue => TrueSubPredicate.Instance;
}
