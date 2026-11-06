using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.DFU.Fixes;

//Salmon size fix, maps to vanilla EntitySalmonSizeFix
//Only large salmon keeps the type field, every other value becomes medium
public sealed class EntitySalmonSizeFix(Schema outputSchema)
    : NamedEntityFix(outputSchema, false, "EntitySalmonSizeFix", References.Entity, "minecraft:salmon")
{
    protected override Typed<object> Fix(Typed<object> entity)
        => entity.Update(DSL.RemainderFinder(), tag =>
        {
            var type = tag.Get("type").AsString("medium");
            return type == "large" ? tag : tag.Set("type", tag.CreateString("medium"));
        });
}
