using NacreCraft.Registry;

namespace NacreCraft.Game.World.Crafting;

//RecipeSerializers built-in recipe serializer registration, maps to vanilla net.minecraft.world.item.crafting.RecipeSerializers
//The loading path dispatches by serializer id, so only the ids and their kinds are registered here
public static class RecipeSerializers
{
    //Ids serializer ids in vanilla registration order
    public static readonly IReadOnlyList<string> Ids = new[]
    {
        ShapedRecipe.SerializerId,
        ShapelessRecipe.SerializerId,
        "crafting_dye",
        "crafting_imbue",
        "crafting_transmute",
        "crafting_decorated_pot",
        "crafting_special_bookcloning",
        "crafting_special_mapextending",
        "crafting_special_firework_rocket",
        "crafting_special_firework_star",
        "crafting_special_firework_star_fade",
        "crafting_special_bannerduplicate",
        "crafting_special_shielddecoration",
        "crafting_special_repairitem",
        SmeltingRecipe.SerializerId,
        BlastingRecipe.SerializerId,
        SmokingRecipe.SerializerId,
        CampfireCookingRecipe.SerializerId,
        StonecutterRecipe.SerializerId,
        "smithing_transform",
        "smithing_trim",
    };

    //Bootstrap registers the serializer ids before the registry freezes, maps to vanilla bootstrap
    public static void Bootstrap()
    {
        foreach (var id in Ids)
        {
            if (BuiltInRegistries.RECIPE_SERIALIZER.ContainsKey(Identifier.WithDefaultNamespace(id))) continue;
            Registry<RecipeSerializer<object>>.Register(BuiltInRegistries.RECIPE_SERIALIZER, id,
                new NamedSerializer(id));
        }
    }

    //NamedSerializer registry entry carrying only the serializer id
    //The serializer body codecs are not attached because recipe loading reads the type field directly
    private sealed class NamedSerializer(string id) : RecipeSerializer<object>
    {
        public string Id => id;

        public override string ToString() => Id;
    }
}
