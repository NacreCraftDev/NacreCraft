using NacreCraft.Game.World.Crafting;
using NacreCraft.Game.World.Effect;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Registry;
using Potion = NacreCraft.Registry.Potion;

namespace NacreCraft.Game.World.Items.Alchemy;

//PotionBrewing the brewing stand recipe table, maps to vanilla net.minecraft.world.item.alchemy.PotionBrewing
//Vanilla filters the recipes by the enabled feature flags; the feature flag system is not ported, so every recipe is kept
public sealed class PotionBrewing
{
    public const int BrewingTimeSeconds = 20;

    public static readonly PotionBrewing Empty =
        new(new List<Ingredient>(), new List<BrewingMix<Potion>>(), new List<BrewingMix<Item>>());

    private readonly List<Ingredient> _containers;
    private readonly List<BrewingMix<Potion>> _potionMixes;
    private readonly List<BrewingMix<Item>> _containerMixes;

    private PotionBrewing(List<Ingredient> containers, List<BrewingMix<Potion>> potionMixes,
        List<BrewingMix<Item>> containerMixes)
    {
        _containers = containers;
        _potionMixes = potionMixes;
        _containerMixes = containerMixes;
    }

    public bool IsIngredient(ItemStack ingredient) => IsContainerIngredient(ingredient) || IsPotionIngredient(ingredient);

    private bool IsContainer(ItemStack input) => _containers.Any(container => container.Matches(input));

    public bool IsContainerIngredient(ItemStack ingredient) => _containerMixes.Any(mix => mix.Ingredient.Matches(ingredient));

    public bool IsPotionIngredient(ItemStack ingredient) => _potionMixes.Any(mix => mix.Ingredient.Matches(ingredient));

    public bool IsBrewablePotion(Holder<Potion> potion) => _potionMixes.Any(mix => Equals(mix.To.Value, potion.Value));

    public bool HasMix(ItemStack source, ItemStack ingredient)
        => IsContainer(source) && (HasContainerMix(source, ingredient) || HasPotionMix(source, ingredient));

    public bool HasContainerMix(ItemStack source, ItemStack ingredient)
        => _containerMixes.Any(mix =>
            ReferenceEquals(source.GetItem(), mix.From.Value) && mix.Ingredient.Matches(ingredient));

    public bool HasPotionMix(ItemStack source, ItemStack ingredient)
    {
        var potion = PotionOf(source);
        if (potion is null) return false;
        return _potionMixes.Any(mix => Equals(mix.From.Value, potion.Value) && mix.Ingredient.Matches(ingredient));
    }

    //Mix brews the ingredient into the source stack, maps to vanilla mix
    public ItemStack Mix(ItemStack ingredient, ItemStack source)
    {
        if (source.IsEmpty()) return source;
        var potion = PotionOf(source);
        if (potion is null) return source;
        foreach (var mix in _containerMixes)
            if (ReferenceEquals(source.GetItem(), mix.From.Value) && mix.Ingredient.Matches(ingredient))
                return PotionContents.CreateItemStack(mix.To.Value, potion);
        foreach (var mix in _potionMixes)
            if (Equals(mix.From.Value, potion.Value) && mix.Ingredient.Matches(ingredient))
                return PotionContents.CreateItemStack(source.GetItem(), mix.To);
        return source;
    }

    //PotionOf the potion a stack carries, null when it carries none
    private static Holder<Potion>? PotionOf(ItemStack stack)
    {
        var contents = stack.GetOrDefault<object>(DataComponents.POTION_CONTENTS, PotionContents.Empty) as PotionContents;
        return contents is not null && contents.Potion.IsPresent ? contents.Potion.Get() : null;
    }

    public static PotionBrewing Bootstrap()
    {
        var builder = new Builder();
        AddVanillaMixes(builder);
        return builder.Build();
    }

    //AddVanillaMixes registers the built-in recipes, maps to vanilla addVanillaMixes
    //Items are resolved by registry name because NC keeps most item constants in the generated table rather than in Items
    public static void AddVanillaMixes(Builder builder)
    {
        var potion = ItemOrNull("potion");
        var splashPotion = ItemOrNull("splash_potion");
        var lingeringPotion = ItemOrNull("lingering_potion");
        if (potion is null || splashPotion is null || lingeringPotion is null) return;

        builder.AddContainer(potion);
        builder.AddContainer(splashPotion);
        builder.AddContainer(lingeringPotion);
        builder.AddContainerRecipe(potion, ItemOrNull("gunpowder"), splashPotion);
        builder.AddContainerRecipe(splashPotion, ItemOrNull("dragon_breath"), lingeringPotion);

        builder.AddMix(Potions.WATER, ItemOrNull("glowstone_dust"), Potions.THICK);
        builder.AddMix(Potions.WATER, ItemOrNull("redstone"), Potions.MUNDANE);
        builder.AddMix(Potions.WATER, ItemOrNull("nether_wart"), Potions.AWKWARD);
        builder.AddStartMix(ItemOrNull("breeze_rod"), Potions.WIND_CHARGED);
        builder.AddStartMix(ItemOrNull("slime_block"), Potions.OOZING);
        builder.AddStartMix(ItemOrNull("stone"), Potions.INFESTED);
        builder.AddStartMix(ItemOrNull("cobweb"), Potions.WEAVING);
        builder.AddMix(Potions.AWKWARD, ItemOrNull("golden_carrot"), Potions.NIGHT_VISION);
        builder.AddMix(Potions.NIGHT_VISION, ItemOrNull("redstone"), Potions.LONG_NIGHT_VISION);
        builder.AddMix(Potions.NIGHT_VISION, ItemOrNull("fermented_spider_eye"), Potions.INVISIBILITY);
        builder.AddMix(Potions.LONG_NIGHT_VISION, ItemOrNull("fermented_spider_eye"), Potions.LONG_INVISIBILITY);
        builder.AddMix(Potions.INVISIBILITY, ItemOrNull("redstone"), Potions.LONG_INVISIBILITY);
        builder.AddStartMix(ItemOrNull("magma_cream"), Potions.FIRE_RESISTANCE);
        builder.AddMix(Potions.FIRE_RESISTANCE, ItemOrNull("redstone"), Potions.LONG_FIRE_RESISTANCE);
        builder.AddStartMix(ItemOrNull("rabbit_foot"), Potions.LEAPING);
        builder.AddMix(Potions.LEAPING, ItemOrNull("redstone"), Potions.LONG_LEAPING);
        builder.AddMix(Potions.LEAPING, ItemOrNull("glowstone_dust"), Potions.STRONG_LEAPING);
        builder.AddMix(Potions.LEAPING, ItemOrNull("fermented_spider_eye"), Potions.SLOWNESS);
        builder.AddMix(Potions.LONG_LEAPING, ItemOrNull("fermented_spider_eye"), Potions.LONG_SLOWNESS);
        builder.AddMix(Potions.SLOWNESS, ItemOrNull("redstone"), Potions.LONG_SLOWNESS);
        builder.AddMix(Potions.SLOWNESS, ItemOrNull("glowstone_dust"), Potions.STRONG_SLOWNESS);
        builder.AddMix(Potions.AWKWARD, ItemOrNull("turtle_helmet"), Potions.TURTLE_MASTER);
        builder.AddMix(Potions.TURTLE_MASTER, ItemOrNull("redstone"), Potions.LONG_TURTLE_MASTER);
        builder.AddMix(Potions.TURTLE_MASTER, ItemOrNull("glowstone_dust"), Potions.STRONG_TURTLE_MASTER);
        builder.AddMix(Potions.SWIFTNESS, ItemOrNull("fermented_spider_eye"), Potions.SLOWNESS);
        builder.AddMix(Potions.LONG_SWIFTNESS, ItemOrNull("fermented_spider_eye"), Potions.LONG_SLOWNESS);
        builder.AddStartMix(ItemOrNull("sugar"), Potions.SWIFTNESS);
        builder.AddMix(Potions.SWIFTNESS, ItemOrNull("redstone"), Potions.LONG_SWIFTNESS);
        builder.AddMix(Potions.SWIFTNESS, ItemOrNull("glowstone_dust"), Potions.STRONG_SWIFTNESS);
        builder.AddMix(Potions.AWKWARD, ItemOrNull("pufferfish"), Potions.WATER_BREATHING);
        builder.AddMix(Potions.WATER_BREATHING, ItemOrNull("redstone"), Potions.LONG_WATER_BREATHING);
        builder.AddStartMix(ItemOrNull("glistering_melon_slice"), Potions.HEALING);
        builder.AddMix(Potions.HEALING, ItemOrNull("glowstone_dust"), Potions.STRONG_HEALING);
        builder.AddMix(Potions.HEALING, ItemOrNull("fermented_spider_eye"), Potions.HARMING);
        builder.AddMix(Potions.STRONG_HEALING, ItemOrNull("fermented_spider_eye"), Potions.STRONG_HARMING);
        builder.AddMix(Potions.HARMING, ItemOrNull("glowstone_dust"), Potions.STRONG_HARMING);
        builder.AddMix(Potions.POISON, ItemOrNull("fermented_spider_eye"), Potions.HARMING);
        builder.AddMix(Potions.LONG_POISON, ItemOrNull("fermented_spider_eye"), Potions.HARMING);
        builder.AddMix(Potions.STRONG_POISON, ItemOrNull("fermented_spider_eye"), Potions.STRONG_HARMING);
        builder.AddStartMix(ItemOrNull("spider_eye"), Potions.POISON);
        builder.AddMix(Potions.POISON, ItemOrNull("redstone"), Potions.LONG_POISON);
        builder.AddMix(Potions.POISON, ItemOrNull("glowstone_dust"), Potions.STRONG_POISON);
        builder.AddStartMix(ItemOrNull("ghast_tear"), Potions.REGENERATION);
        builder.AddMix(Potions.REGENERATION, ItemOrNull("redstone"), Potions.LONG_REGENERATION);
        builder.AddMix(Potions.REGENERATION, ItemOrNull("glowstone_dust"), Potions.STRONG_REGENERATION);
        builder.AddStartMix(ItemOrNull("blaze_powder"), Potions.STRENGTH);
        builder.AddMix(Potions.STRENGTH, ItemOrNull("redstone"), Potions.LONG_STRENGTH);
        builder.AddMix(Potions.STRENGTH, ItemOrNull("glowstone_dust"), Potions.STRONG_STRENGTH);
        builder.AddMix(Potions.WATER, ItemOrNull("fermented_spider_eye"), Potions.WEAKNESS);
        builder.AddMix(Potions.WEAKNESS, ItemOrNull("redstone"), Potions.LONG_WEAKNESS);
        builder.AddMix(Potions.AWKWARD, ItemOrNull("phantom_membrane"), Potions.SLOW_FALLING);
        builder.AddMix(Potions.SLOW_FALLING, ItemOrNull("redstone"), Potions.LONG_SLOW_FALLING);
    }

    //ItemOrNull resolves an item by name, null when the item is not registered
    private static Item? ItemOrNull(string name)
    {
        var id = Identifier.WithDefaultNamespace(name);
        return BuiltInRegistries.ITEM.ContainsKey(id) ? BuiltInRegistries.ITEM.GetValue(id) : null;
    }

    //BrewingMix one brewing recipe, maps to vanilla PotionBrewing.Mix
    private sealed record BrewingMix<T>(Holder<T> From, Ingredient Ingredient, Holder<T> To) where T : class;

    //Builder collects the recipes, maps to vanilla PotionBrewing.Builder
    public sealed class Builder
    {
        private readonly List<Ingredient> _containers = new();
        private readonly List<BrewingMix<Potion>> _potionMixes = new();
        private readonly List<BrewingMix<Item>> _containerMixes = new();

        //ExpectPotion every container must be a potion item, maps to vanilla expectPotion
        //NC has no PotionItem type yet, so the check is skipped
        private static void ExpectPotion(Item item) { }

        public void AddContainerRecipe(Item? from, Item? ingredient, Item? to)
        {
            if (from is null || ingredient is null || to is null) return;
            ExpectPotion(from);
            ExpectPotion(to);
            _containerMixes.Add(new BrewingMix<Item>(from.BuiltInRegistryHolder, Ingredient.Of(ingredient), to.BuiltInRegistryHolder));
        }

        public void AddContainer(Item? item)
        {
            if (item is null) return;
            ExpectPotion(item);
            _containers.Add(Ingredient.Of(item));
        }

        public void AddMix(Holder<Potion> from, Item? ingredient, Holder<Potion> to)
        {
            if (ingredient is null) return;
            _potionMixes.Add(new BrewingMix<Potion>(from, Ingredient.Of(ingredient), to));
        }

        //AddStartMix a recipe that starts from awkward, maps to vanilla addStartMix
        public void AddStartMix(Item? ingredient, Holder<Potion> potion)
        {
            AddMix(Potions.WATER, ingredient, Potions.MUNDANE);
            AddMix(Potions.AWKWARD, ingredient, potion);
        }

        public PotionBrewing Build()
            => new(new List<Ingredient>(_containers), new List<BrewingMix<Potion>>(_potionMixes),
                new List<BrewingMix<Item>>(_containerMixes));
    }
}
