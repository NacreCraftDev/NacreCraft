using NacreCraft.DataFixer.Schemas;
using T = NacreCraft.DataFixer.Types;

namespace NacreCraft.Game.DFU.Fixes;

using NacreCraft.DataFixer.Fixes;

using NacreCraft.DataFixer;

//Projectile stored weapon fix, maps to vanilla ProjectileStoredWeaponFix
//1.21.4 runs the write+fix+read flow for the arrow/spectral_arrow entities and applies an identity fix for the type conversion
public class ProjectileStoredWeaponFix : DataFix
{
    public ProjectileStoredWeaponFix(Schema outputSchema) : base(outputSchema, true) { }

    protected override TypeRewriteRule MakeRule()
    {
        var inputEntityType = GetInputSchema().GetType(References.Entity);
        var outputEntityType = GetOutputSchema().GetType(References.Entity);
        return FixTypeEverywhereTyped("Fix Arrow stored weapon", inputEntityType, outputEntityType,
            ExtraDataFixUtils.ChainAllFilters(FixChoice("minecraft:arrow"), FixChoice("minecraft:spectral_arrow")));
    }

    //fixChoice builds a named choice lookup by entity name and does the write+read type conversion
    private Func<Typed<object>, Typed<object>> FixChoice(string entityName)
    {
        var inputEntityChoiceType = GetInputSchema().GetChoiceType(References.Entity, entityName);
        var outputEntityChoiceType = GetOutputSchema().GetChoiceType(References.Entity, entityName);
        var entityF = DSL.NamedChoice(entityName, inputEntityChoiceType);
        return input => input.UpdateTyped(entityF, outputEntityChoiceType,
            typed => DataFixUtils.WriteAndReadTypedOrThrow<object, object>(typed, outputEntityChoiceType, d => d));
    }
}
