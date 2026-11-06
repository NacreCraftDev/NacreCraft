using NacreCraft.Codec;
using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.DFU.Fixes;

//Lock predicate component fix, maps to vanilla LockComponentPredicateFix
//1.21.2 turns the string lock into a lock component holding a custom_name component
public sealed class LockComponentPredicateFix(Schema outputSchema)
    : DataComponentRemainderFix(outputSchema, "LockComponentPredicateFix", "minecraft:lock")
{
    protected override Dynamic<object> FixComponent(Dynamic<object> input) => FixLock(input)!;

    //FixLock the shared conversion used by the block entity fix, maps to vanilla fixLock
    public static Dynamic<object>? FixLock(Dynamic<object> input)
    {
        var name = input.AsString().Result();
        if (!name.IsPresent || name.Get().Length == 0) return null;
        var escaped = name.Get().Replace("\\", "\\\\").Replace("\"", "\\\"");
        var nameComponent = input.CreateString("\"" + escaped + "\"");
        var components = input.EmptyMap().Set("minecraft:custom_name", nameComponent);
        return input.EmptyMap().Set("components", components);
    }
}
