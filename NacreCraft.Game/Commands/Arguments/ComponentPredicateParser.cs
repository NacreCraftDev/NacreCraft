using NacreCraft.Codec;
using NacreCraft.Commands;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Util;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ComponentPredicateParser item component predicate grammar, maps to vanilla net.minecraft.commands.arguments.item.ComponentPredicateParser
//Grammar: type[...conditions]; conditions are ANDed, | means OR inside one condition, ! negates one term
//Vanilla builds a packrat grammar whose rules report through a Context; this port keeps the same rules as recursive descent and reports through the same Context
public static class ComponentPredicateParser
{
    public static readonly DynamicCommandExceptionType ErrorUnknownComponent =
        new(id => new LiteralMessage($"unknown component {id}"));

    private static RegistryOps<Tag>? _registryOps;
    private static TagParser<Tag>? _componentTagParser;

    private static RegistryOps<Tag> RegistryOpsForCommands
        => _registryOps ??= new RegistryOps<Tag>(NbtOps.Instance, BuiltInRegistries.CreateRegistryAccess());

    private static TagParser<Tag> ComponentTagParser
        => _componentTagParser ??= TagParser<Tag>.Create(RegistryOpsForCommands);

    //Parse reads a whole predicate expression through the context, maps to the top rule of the vanilla grammar
    public static T Parse<T, C, P>(Context<T, C, P> context, StringReader reader)
        where C : class
        where P : class
    {
        var elementType = ParseAnyType(context, reader);
        if (!reader.CanRead() || reader.Peek() != '[') return elementType;
        reader.Skip();
        T? conditions = default;
        var hasConditions = false;
        if (reader.CanRead() && reader.Peek() != ']')
        {
            conditions = ParseConditions(context, reader);
            hasConditions = true;
        }
        reader.Expect(']');
        if (!hasConditions) return elementType;
        return context.AllOf(new[] { elementType, conditions! });
    }

    //ParseAnyType item identifier, #tag or * wildcard, maps to the any_type / element_type / tag_type rules
    private static T ParseAnyType<T, C, P>(Context<T, C, P> context, StringReader reader)
        where C : class
        where P : class
    {
        //* matches any item
        if (reader.CanRead() && reader.Peek() == '*')
        {
            reader.Skip();
            return context.AnyItem();
        }
        if (reader.CanRead() && reader.Peek() == '#')
        {
            reader.Skip();
            var tagStart = reader.Cursor;
            var id = IdentifierArgument.ReadIdentifier(reader);
            return context.ForTagType(reader, id, tagStart);
        }
        var itemStart = reader.Cursor;
        var itemId = IdentifierArgument.ReadIdentifier(reader);
        return context.ForElementType(reader, itemId, itemStart);
    }

    //ParseConditions all comma-separated conditions must hold, maps to the conditions rule
    private static T ParseConditions<T, C, P>(Context<T, C, P> context, StringReader reader)
        where C : class
        where P : class
    {
        var conditions = new List<T> { ParseAlternatives(context, reader) };
        while (reader.CanRead() && reader.Peek() == ',')
        {
            reader.Skip();
            conditions.Add(ParseAlternatives(context, reader));
        }
        return conditions.Count == 1 ? conditions[0] : context.AllOf(conditions);
    }

    //ParseAlternatives any of the pipe-separated candidates must hold, maps to the alternatives rule
    private static T ParseAlternatives<T, C, P>(Context<T, C, P> context, StringReader reader)
        where C : class
        where P : class
    {
        var alternatives = new List<T> { ParseTerm(context, reader) };
        while (reader.CanRead() && reader.Peek() == '|')
        {
            reader.Skip();
            alternatives.Add(ParseTerm(context, reader));
        }
        return alternatives.Count == 1 ? alternatives[0] : context.AnyOf(alternatives);
    }

    //ParseTerm a negation prefix followed by a single condition, maps to the term / negation rules
    private static T ParseTerm<T, C, P>(Context<T, C, P> context, StringReader reader)
        where C : class
        where P : class
    {
        if (!reader.CanRead() || reader.Peek() != '!') return ParseTest(context, reader);
        reader.Skip();
        return context.Negate(ParseTest(context, reader));
    }

    //ParseTest component value match, predicate match or component existence, maps to the three branches of the test rule
    //The branch order matches vanilla: component_type '=' tag / predicate_type '~' tag / component_type
    private static T ParseTest<T, C, P>(Context<T, C, P> context, StringReader reader)
        where C : class
        where P : class
    {
        var start = reader.Cursor;
        var id = IdentifierArgument.ReadIdentifier(reader);
        //Pseudo components such as count are checked before the component registry
        if (context.TryCreatePseudoTest(reader, id, start, out var pseudo)) return pseudo;
        if (context.LookupComponentType(reader, id) is { } componentType)
        {
            if (reader.CanRead() && reader.Peek() == '=')
            {
                reader.Skip();
                return context.CreateComponentTest(reader, componentType, ReadNbt(reader), start);
            }
            return context.CreateComponentTest(reader, componentType);
        }
        if (context.LookupPredicateType(reader, id) is { } predicateType
            && reader.CanRead() && reader.Peek() == '~')
        {
            reader.Skip();
            return context.CreatePredicateTest(reader, predicateType, ReadNbt(reader), start);
        }
        reader.SetCursor(start);
        throw ErrorUnknownComponent.CreateWithContext(reader, id.ToString());
    }

    //ParseTag reads one SNBT argument value through the shared tag parser, maps to the tag rule of the vanilla grammar
    public static Tag ParseTag(StringReader reader)
    {
        var nbtReader = new CommandStringReader(reader.String) { Cursor = reader.Cursor };
        var tag = ComponentTagParser.ParseAsArgument(nbtReader);
        reader.SetCursor(nbtReader.Cursor);
        return tag;
    }

    //ReadNbt parses SNBT with a separate reader and syncs the cursor back to the command reader
    private static Tag ReadNbt(StringReader reader) => ParseTag(reader);

    //Context callbacks the grammar reports through, maps to vanilla ComponentPredicateParser.Context
    public interface Context<T, C, P>
        where C : class
        where P : class
    {
        //AnyItem the wildcard predicate, maps to the any_type rule
        T AnyItem();

        //ForElementType resolves an item identifier, maps to the element_type rule; start is the identifier start for error reporting
        T ForElementType(StringReader reader, Identifier id, int start);

        IEnumerable<Identifier> ListElementTypes();

        //ForTagType resolves an item tag, maps to the tag_type rule; start is the identifier start for error reporting
        T ForTagType(StringReader reader, Identifier id, int start);

        IEnumerable<Identifier> ListTagTypes();

        C? LookupComponentType(StringReader reader, Identifier id);

        IEnumerable<Identifier> ListComponentTypes();

        //CreateComponentTest value match on a component, start is the term start used for error reporting
        T CreateComponentTest(StringReader reader, C type, Tag value, int start);

        //CreateComponentTest existence test on a component
        T CreateComponentTest(StringReader reader, C type);

        //TryCreatePseudoTest handles a pseudo component such as count; false when the id is not one
        bool TryCreatePseudoTest(StringReader reader, Identifier id, int start, out T test);

        P? LookupPredicateType(StringReader reader, Identifier id);

        IEnumerable<Identifier> ListPredicateTypes();

        //CreatePredicateTest test carried by a component predicate type, start is the term start used for error reporting
        T CreatePredicateTest(StringReader reader, P type, Tag value, int start);

        T Negate(T value);

        T AnyOf(IReadOnlyList<T> values);

        T AllOf(IReadOnlyList<T> values);
    }
}
