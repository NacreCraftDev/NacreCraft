using NacreCraft.Codec;
using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.Commands;
using NacreCraft.Game.World.Items;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Util;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ItemPredicateArgument item predicate argument, maps to vanilla net.minecraft.commands.arguments.item.ItemPredicateArgument
//Syntax: <item> | #<tag> | *, optionally followed by [condition,condition...]
//Conditions are ANDed; within a condition | means OR and ! negates
//Three condition forms: <component>=<SNBT value> value match / <component> alone means existence / <predicate>~<SNBT value>
//The grammar itself lives in ComponentPredicateParser; this class supplies the item, tag, component and predicate lookups
public sealed class ItemPredicateArgument
    : ArgumentType<Predicate<ItemStack>>,
        ComponentPredicateParser.Context<Predicate<ItemStack>, DataComponentType<object>, DataComponentPredicate.Type>
{
    //CountId count pseudo-component id, maps to vanilla ItemPredicateArgument.COUNT_ID
    //count is not in the component registry and is judged by a count range on its own
    private static readonly Identifier CountId = Identifier.WithDefaultNamespace("count");

    private static readonly IReadOnlyList<string> ExamplesList =
        new[] { "stick", "minecraft:stick", "#stick", "#stick[foo='bar']" };

    public static readonly DynamicCommandExceptionType ErrorUnknownItem =
        new(id => new LiteralMessage($"unknown item {id}"));

    public static readonly DynamicCommandExceptionType ErrorUnknownTag =
        new(id => new LiteralMessage($"unknown item tag {id}"));

    public static readonly Dynamic2CommandExceptionType ErrorMalformedComponent =
        new((type, message) => new LiteralMessage($"component {type} malformed: {message}"));

    private static RegistryOps<Tag>? _registryOps;

    //RegistryOpsForCommands lazily constructed; the registry is only needed when parsing component values
    private static RegistryOps<Tag> RegistryOpsForCommands
        => _registryOps ??= new RegistryOps<Tag>(NbtOps.Instance, BuiltInRegistries.CreateRegistryAccess());

    //ItemPredicate creates a predicate argument instance, used for command tree registration
    public static ItemPredicateArgument ItemPredicate() => new();

    public Predicate<ItemStack> Parse(StringReader reader)
    {
        var start = reader.Cursor;
        try
        {
            return ComponentPredicateParser.Parse(this, reader);
        }
        catch (CommandSyntaxException)
        {
            reader.SetCursor(start);
            throw;
        }
    }

    public Predicate<ItemStack> AnyItem() => _ => true;

    public Predicate<ItemStack> ForElementType(StringReader reader, Identifier id, int start)
    {
        //The registry is defaulted, so look up by ResourceKey; otherwise an unknown item silently falls back to the default
        var item = BuiltInRegistries.ITEM.GetValue(ResourceKey<Item>.Create(Registries.ITEM, id));
        if (item is null)
        {
            reader.SetCursor(start);
            throw ErrorUnknownItem.CreateWithContext(reader, id.ToString());
        }
        return stack => !stack.IsEmpty() && ReferenceEquals(stack.GetItem(), item);
    }

    public IEnumerable<Identifier> ListElementTypes() => BuiltInRegistries.ITEM.KeySet;

    public Predicate<ItemStack> ForTagType(StringReader reader, Identifier id, int start)
    {
        var key = TagKey<Item>.Create(Registries.ITEM, id);
        var set = BuiltInRegistries.ITEM.Get(key);
        if (set is null)
        {
            reader.SetCursor(start);
            throw ErrorUnknownTag.CreateWithContext(reader, id.ToString());
        }
        return stack => !stack.IsEmpty() && set.Contains(stack.GetTypeHolder()!);
    }

    public IEnumerable<Identifier> ListTagTypes()
        => BuiltInRegistries.ITEM.GetTags().Select(set => set.Key.Location);

    //LookupComponentType looks up a persistent component type by identifier; a non-persistent component is reported unknown like vanilla
    public DataComponentType<object>? LookupComponentType(StringReader reader, Identifier id)
        => BuiltInRegistries.DATA_COMPONENT_TYPE.GetValue(id) is DataComponentType<object> type && !type.IsTransient
            ? type
            : null;

    public IEnumerable<Identifier> ListComponentTypes() => BuiltInRegistries.DATA_COMPONENT_TYPE.KeySet;

    public Predicate<ItemStack> CreateComponentTest(StringReader reader, DataComponentType<object> type, Tag value,
        int start)
    {
        var parsed = type.CodecOrThrow().Parse(RegistryOpsForCommands, value);
        return parsed.MapOrElse<Predicate<ItemStack>>(
            expected =>
            {
                //Value match semantics are an exact predicate, maps to vanilla DataComponentExactPredicate.expect(type, value)
                var exact = DataComponentExactPredicate.Expect(type, expected);
                return (Predicate<ItemStack>)(stack => !stack.IsEmpty() && exact.Test(stack.GetComponents()));
            },
            message =>
            {
                reader.SetCursor(start);
                throw ErrorMalformedComponent.CreateWithContext(reader, type.ToString()!, message);
            });
    }

    public Predicate<ItemStack> CreateComponentTest(StringReader reader, DataComponentType<object> type)
        => stack => !stack.IsEmpty() && stack.GetComponents().Get(type) is not null;

    //TryCreatePseudoTest handles the count pseudo-component
    //The value is an integer range and is always true when present alone, maps to count in vanilla PSEUDO_COMPONENTS
    public bool TryCreatePseudoTest(StringReader reader, Identifier id, int start, out Predicate<ItemStack> test)
    {
        test = _ => false;
        if (id != CountId) return false;
        if (reader.CanRead() && reader.Peek() == '=')
        {
            reader.Skip();
            var range = ReadCountRange(reader, start, ReadNbt(reader));
            test = stack => !stack.IsEmpty() && range.Matches(stack.GetCount());
            return true;
        }
        test = _ => true;
        return true;
    }

    //LookupPredicateType looks up the predicate type by identifier, maps to vanilla lookupPredicateType
    //The component type side does not go through here; component existence and value matching are handled in the other branches
    public DataComponentPredicate.Type? LookupPredicateType(StringReader reader, Identifier id)
        => BuiltInRegistries.DATA_COMPONENT_PREDICATE_TYPE.GetValue(id) as DataComponentPredicate.Type;

    public IEnumerable<Identifier> ListPredicateTypes() => BuiltInRegistries.DATA_COMPONENT_PREDICATE_TYPE.KeySet;

    public Predicate<ItemStack> CreatePredicateTest(StringReader reader, DataComponentPredicate.Type type, Tag value,
        int start)
    {
        var parsed = type.Codec.Parse(RegistryOpsForCommands, value);
        if (!parsed.Result().IsPresent)
        {
            reader.SetCursor(start);
            throw ErrorMalformedComponent.CreateWithContext(reader, type.ToString()!, "invalid predicate value");
        }
        var predicateValue = parsed.GetOrThrow();
        return stack => !stack.IsEmpty() && type.Matches(stack.GetComponents(), predicateValue);
    }

    public Predicate<ItemStack> Negate(Predicate<ItemStack> value) => stack => !value(stack);

    public Predicate<ItemStack> AnyOf(IReadOnlyList<Predicate<ItemStack>> values)
        => stack => values.Any(test => test(stack));

    public Predicate<ItemStack> AllOf(IReadOnlyList<Predicate<ItemStack>> values)
        => stack => values.All(test => test(stack));

    //ReadCountRange the value of the count pseudo-component; a single integer treats both bounds as equal, a compound tag reads min/max
    private static MinMaxBounds.Ints ReadCountRange(StringReader reader, int start, Tag tag)
    {
        if (tag is IntTag intTag)
            return new MinMaxBounds.Ints(intTag.Value, intTag.Value);
        if (tag is CompoundTag compound && (compound.Contains("min") || compound.Contains("max")))
            return new MinMaxBounds.Ints(compound.GetInt("min")?.Value, compound.GetInt("max")?.Value);
        reader.SetCursor(start);
        throw ErrorMalformedComponent.CreateWithContext(reader, CountId.ToString(),
            "expected an integer or a min/max range");
    }

    //ReadNbt parses SNBT with a separate reader and syncs the cursor back to the command reader
    private static Tag ReadNbt(StringReader reader) => ComponentPredicateParser.ParseTag(reader);

    //GetItemPredicate gets the parsed predicate
    public static Predicate<ItemStack> GetItemPredicate(CommandContext<CommandSourceStack> context, string name)
        => context.GetArgument<Predicate<ItemStack>>(name);

    //ListSuggestions suggests items or tags for the current cursor position, maps to vanilla ResourceLookupRule suggestions
    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
    {
        var remaining = builder.Remaining;
        if (remaining.StartsWith('#'))
        {
            foreach (var set in BuiltInRegistries.ITEM.GetTags())
            {
                var text = "#" + set.Key.Location.ToShortString();
                if (text.StartsWith(remaining, StringComparison.Ordinal))
                    builder.Add(text);
            }
        }
        else
        {
            foreach (var id in BuiltInRegistries.ITEM.KeySet)
            {
                var text = id.ToShortString();
                if (text.StartsWith(remaining, StringComparison.Ordinal))
                    builder.Add(text);
            }
            //Tags and the wildcard are also suggested, maps to vanilla listTagTypes and all_type
            if ("#".StartsWith(remaining, StringComparison.Ordinal)) builder.Add("#");
            if ("*".StartsWith(remaining, StringComparison.Ordinal)) builder.Add("*");
        }
        return builder.BuildFuture();
    }

    public IReadOnlyList<string> Examples => ExamplesList;
}
