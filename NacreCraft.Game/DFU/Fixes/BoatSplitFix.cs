using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.DFU.Fixes;

//Boat split fix, maps to vanilla BoatSplitFix
//1.21.2 splits the generic boat/chest_boat entities into one entity id per wood variant
public sealed class BoatSplitFix(Schema outputSchema) : DataFix(outputSchema, true)
{
    private static bool IsNormalBoat(string id) => id == "minecraft:boat";

    private static bool IsChestBoat(string id) => id == "minecraft:chest_boat";

    private static bool IsAnyBoat(string id) => IsNormalBoat(id) || IsChestBoat(id);

    private static string MapVariantToNormalBoat(string id) => id switch
    {
        "spruce" => "minecraft:spruce_boat",
        "birch" => "minecraft:birch_boat",
        "jungle" => "minecraft:jungle_boat",
        "acacia" => "minecraft:acacia_boat",
        "cherry" => "minecraft:cherry_boat",
        "dark_oak" => "minecraft:dark_oak_boat",
        "mangrove" => "minecraft:mangrove_boat",
        "bamboo" => "minecraft:bamboo_raft",
        _ => "minecraft:oak_boat"
    };

    private static string MapVariantToChestBoat(string id) => id switch
    {
        "spruce" => "minecraft:spruce_chest_boat",
        "birch" => "minecraft:birch_chest_boat",
        "jungle" => "minecraft:jungle_chest_boat",
        "acacia" => "minecraft:acacia_chest_boat",
        "cherry" => "minecraft:cherry_chest_boat",
        "dark_oak" => "minecraft:dark_oak_chest_boat",
        "mangrove" => "minecraft:mangrove_chest_boat",
        "bamboo" => "minecraft:bamboo_chest_raft",
        _ => "minecraft:oak_chest_boat"
    };

    protected override TypeRewriteRule MakeRule()
    {
        var idFinder = DSL.FieldFinder("id", NamespacedSchema.NamespacedString());
        var oldType = GetInputSchema().GetType(References.Entity);
        var newType = GetOutputSchema().GetType(References.Entity);
        return FixTypeEverywhereTyped("BoatSplitFix", oldType, newType, input =>
        {
            var id = input.GetOptional(idFinder);
            if (id.IsPresent && IsAnyBoat(id.Get()))
            {
                var tag = input.GetOrCreate(DSL.RemainderFinder());
                var maybeBoatId = tag.Get("Type").AsString().Result();
                var newId = IsChestBoat(id.Get())
                    ? (maybeBoatId.IsPresent ? MapVariantToChestBoat(maybeBoatId.Get()) : "minecraft:oak_chest_boat")
                    : (maybeBoatId.IsPresent ? MapVariantToNormalBoat(maybeBoatId.Get()) : "minecraft:oak_boat");
                return ExtraDataFixUtils.Cast(newType, input)
                    .Update(DSL.RemainderFinder(), remainder => remainder.Remove("Type"))
                    .Set(idFinder, newId);
            }
            return ExtraDataFixUtils.Cast(newType, input);
        });
    }
}
