using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.DFU.Fixes;

//Container block entity lock predicate fix, maps to vanilla ContainerBlockEntityLockPredicateFix
//Renames the block entity Lock field to lock and converts its value into the component form
public sealed class ContainerBlockEntityLockPredicateFix(Schema outputSchema) : DataFix(outputSchema, false)
{
    protected override TypeRewriteRule MakeRule()
        => FixTypeEverywhereTyped("ContainerBlockEntityLockPredicateFix",
            GetInputSchema().FindChoiceType(References.BlockEntity),
            entity => entity.Update(DSL.RemainderFinder(), FixTag));

    private static Dynamic<object> FixTag(Dynamic<object> tag)
    {
        var lockValue = tag.Get("Lock").Result();
        var removed = tag.Remove("Lock");
        if (!lockValue.IsPresent) return removed;
        var fixedValue = LockComponentPredicateFix.FixLock(lockValue.Get());
        return fixedValue is null ? removed : removed.Set("lock", fixedValue);
    }
}
