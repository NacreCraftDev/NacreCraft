namespace NacreCraft.Registry;

//GameRuleTypeVisitor visitor over the rule kinds, maps to vanilla net.minecraft.world.level.gamerules.GameRuleTypeVisitor
//The generic entry point catches every rule, the typed ones only the matching kind; every method defaults to doing nothing
//Vanilla types the two kinds as GameRule<Boolean>/GameRule<Integer>; NC stores every rule as GameRule<object>, so they stay generic
public interface GameRuleTypeVisitor
{
    void Visit<T>(GameRule<T> gameRule) { }

    void VisitBoolean<T>(GameRule<T> gameRule) { }

    void VisitInteger<T>(GameRule<T> gameRule) { }
}
