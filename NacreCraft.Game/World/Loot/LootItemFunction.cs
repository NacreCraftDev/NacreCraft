using NacreCraft.Codec;
using NacreCraft.Game.World.Items;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Game.World.Loot;

//LootItemFunction rewrites a stack a pool produced, maps to vanilla LootItemFunction
public interface LootItemFunction
{
    //Conditions gate the whole rewrite, maps to vanilla LootItemConditionalFunction.conditions
    IReadOnlyList<LootItemCondition> Conditions { get; }

    ItemStack Apply(ItemStack stack, LootContext context);

    //TypeId is the registry name the function field dispatches on
    Identifier TypeId { get; }

    MapCodec<LootItemFunction> Codec();
}

//LootItemConditionalFunction a function that runs only when its conditions pass, maps to vanilla
//LootItemConditionalFunction
public abstract record LootItemConditionalFunction(IReadOnlyList<LootItemCondition> Conditions) : LootItemFunction
{
    public ItemStack Apply(ItemStack stack, LootContext context)
        => Conditions.All(condition => condition.Test(context)) ? Run(stack, context) : stack;

    public abstract Identifier TypeId { get; }

    public abstract MapCodec<LootItemFunction> Codec();

    //Run rewrites the stack once every condition passed, maps to vanilla run
    protected abstract ItemStack Run(ItemStack stack, LootContext context);
}

//LootItemFunctions the function type registry plus the root codec, maps to vanilla LootItemFunctions
public static class LootItemFunctions
{
    private static readonly Dictionary<Identifier, MapCodec<LootItemFunction>> Types = new();

    //TYPED_CODEC dispatches on the function field, the shape vanilla writes for a typed function
    public static readonly Codec<LootItemFunction> TYPED_CODEC =
        IdentifierCodec.Instance.Dispatch<LootItemFunction, Identifier>("function", f => f.TypeId, Lookup);

    //Compose folds a function list into a single transform, maps to vanilla LootItemFunctions.compose
    public static Func<ItemStack, LootContext, ItemStack> Compose(IReadOnlyList<LootItemFunction> functions)
    {
        if (functions.Count == 0) return static (stack, _) => stack;
        var terms = functions.ToArray();
        if (terms.Length == 1)
        {
            var only = terms[0];
            return (stack, context) => only.Apply(stack, context);
        }
        return (stack, context) =>
        {
            foreach (var function in terms)
                stack = function.Apply(stack, context);
            return stack;
        };
    }

    //Decorate wraps the consumer so every stack a pool emits passes through the composed functions first
    public static Action<ItemStack> Decorate(Func<ItemStack, LootContext, ItemStack> composite,
        Action<ItemStack> output, LootContext context)
        => stack => output(composite(stack, context));

    static LootItemFunctions()
    {
        //Registration order follows vanilla LootItemFunctions.bootstrap
        Register("set_count", SetCountFunction.MAP_CODEC);
        Register("set_damage", SetDamageFunction.MAP_CODEC);
        Register("limit_count", LimitCountFunction.MAP_CODEC);
        Register("apply_bonus", ApplyBonusFunction.MAP_CODEC);
        Register("set_components", SetComponentsFunction.MAP_CODEC);
        Register("copy_components", CopyComponentsFunction.MAP_CODEC);
        Register("copy_state", CopyStateFunction.MAP_CODEC);
        Register("set_name", SetNameFunction.MAP_CODEC);
        Register("exploration_map", ExplorationMapFunction.MAP_CODEC);
        Register("set_stew_effect", SetStewEffectFunction.MAP_CODEC);
        Register("set_random_dyes", SetRandomDyesFunction.MAP_CODEC);
        Register("set_random_potion", SetRandomPotionFunction.MAP_CODEC);
        Register("set_potion", SetPotionFunction.MAP_CODEC);
        Register("set_ominous_bottle_amplifier", SetOminousBottleAmplifierFunction.MAP_CODEC);
        Register("set_enchantments", SetEnchantmentsFunction.MAP_CODEC);
        Register("enchant_randomly", EnchantRandomlyFunction.MAP_CODEC);
        Register("enchant_with_levels", EnchantWithLevelsFunction.MAP_CODEC);
        Register("enchanted_count_increase", EnchantedCountIncreaseFunction.MAP_CODEC);
        Register("furnace_smelt", FurnaceSmeltFunction.MAP_CODEC);
        Register("set_instrument", SetInstrumentFunction.MAP_CODEC);
        Register("explosion_decay", ExplosionDecayFunction.MAP_CODEC);
        Register("discard", DiscardFunction.MAP_CODEC);
        Register("filtered", FilteredFunction.MAP_CODEC);
    }

    private static void Register(string name, MapCodec<LootItemFunction> codec)
        => Types[Identifier.WithDefaultNamespace(name)] = codec;

    private static MapCodec<LootItemFunction> Lookup(Identifier id)
        => Types.TryGetValue(id, out var codec)
            ? codec
            : throw new KeyNotFoundException($"Unknown loot function type: {id}");
}
