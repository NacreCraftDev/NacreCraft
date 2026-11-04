using NacreCraft.Codec;
using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.World.Items.Component;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Util;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ItemParser item argument parse engine, maps to vanilla net.minecraft.commands.arguments.item.ItemParser
//Syntax: <item identifier>[<component>=<SNBT value>,!<component>,...]; component values go through SNBT first then the component's own persistent Codec
//The same walk feeds both value parsing and suggestions, so the two paths cannot drift apart
public sealed class ItemParser
{
    public const char SyntaxStartComponents = '[';
    public const char SyntaxEndComponents = ']';
    public const char SyntaxComponentSeparator = ',';
    public const char SyntaxComponentAssignment = '=';
    public const char SyntaxRemovedComponent = '!';

    public static readonly DynamicCommandExceptionType ErrorUnknownItem =
        new(name => new LiteralMessage($"unknown item {name}"));

    public static readonly DynamicCommandExceptionType ErrorUnknownComponent =
        new(name => new LiteralMessage($"unknown component {name}"));

    public static readonly Dynamic2CommandExceptionType ErrorMalformedComponent =
        new((type, message) => new LiteralMessage($"component {type} malformed: {message}"));

    public static readonly SimpleCommandExceptionType ErrorExpectedComponent =
        new(new LiteralMessage("missing component"));

    public static readonly DynamicCommandExceptionType ErrorRepeatedComponent =
        new(name => new LiteralMessage($"duplicate component {name}"));

    public static readonly IReadOnlyList<string> ExamplesList =
        new[] { "stick", "minecraft:stick", "stick[damage=1]" };

    private static RegistryOps<Tag>? _registryOps;
    private static TagParser<Tag>? _componentTagParser;

    //RegistryOpsForCommands lazily constructed; the registry is only needed when parsing component values
    private static RegistryOps<Tag> RegistryOpsForCommands
        => _registryOps ??= new RegistryOps<Tag>(NbtOps.Instance, BuiltInRegistries.CreateRegistryAccess());

    private static TagParser<Tag> ComponentTagParser
        => _componentTagParser ??= TagParser<Tag>.Create(RegistryOpsForCommands);

    //Parse walks the argument and returns the item with its component patch
    public ItemInput Parse(StringReader reader)
    {
        var cursor = reader.Cursor;
        try
        {
            return new State(reader, new SilentVisitor()).ParseAll();
        }
        catch (CommandSyntaxException)
        {
            reader.SetCursor(cursor);
            throw;
        }
    }

    //Parse walks the argument and reports every recognized element to the visitor
    public void Parse(StringReader reader, Visitor visitor)
    {
        var cursor = reader.Cursor;
        try
        {
            new State(reader, visitor).ParseAll();
        }
        catch (CommandSyntaxException)
        {
            reader.SetCursor(cursor);
            throw;
        }
    }

    //FillSuggestions runs the same walk and suggests at the point where it stopped, maps to vanilla fillSuggestions
    public Task<Suggestions> FillSuggestions(SuggestionsBuilder builder)
    {
        var reader = new StringReader(builder.Input);
        reader.SetCursor(builder.Start);
        var visitor = new SuggestionsVisitor();
        try
        {
            new State(reader, visitor).ParseAll();
        }
        catch (CommandSyntaxException)
        {
        }
        //Suggestions are based on the current cursor offset, maps to vanilla resolveSuggestions
        var offset = builder.CreateOffset(reader.Cursor);
        visitor.Suggest(offset);
        return offset.BuildFuture();
    }

    //Visitor receive callbacks during the walk, maps to vanilla ItemParser.Visitor
    public interface Visitor
    {
        void VisitItem(Item item) { }

        void VisitComponent(DataComponentType<object> type, object value) { }

        void VisitRemovedComponent(DataComponentType<object> type) { }

        void VisitSuggestions(Action<SuggestionsBuilder> suggestions) { }
    }

    //SilentVisitor discards every callback, used by the plain value path
    private sealed class SilentVisitor : Visitor
    {
    }

    //State one walk over the argument, maps to the vanilla inner State
    private sealed class State(StringReader reader, Visitor visitor)
    {
        private readonly Dictionary<object, Optional<object>> _components = new();
        private readonly HashSet<DataComponentType<object>> _seen = new(ReferenceEqualityComparer.Instance);
        private Identifier _itemId;
        private Item _item = null!;

        //ParseAll reads the item then the optional component list, maps to vanilla State.parse
        public ItemInput ParseAll()
        {
            visitor.VisitSuggestions(SuggestItem);
            ReadItem();
            visitor.VisitSuggestions(SuggestStartComponents);
            if (reader.CanRead() && reader.Peek() == SyntaxStartComponents)
            {
                visitor.VisitSuggestions(_ => { });
                ReadComponents();
            }
            return new ItemInput(_item, _itemId, new DataComponentPatch(_components));
        }

        private void ReadItem()
        {
            var cursor = reader.Cursor;
            var id = IdentifierArgument.ReadIdentifier(reader);
            //Look up by ResourceKey; the item registry is defaulted, so looking up an unknown item by Identifier falls back to the default
            var item = BuiltInRegistries.ITEM.GetValue(ResourceKey<Item>.Create(Registries.ITEM, id));
            if (item is null)
            {
                reader.SetCursor(cursor);
                throw ErrorUnknownItem.CreateWithContext(reader, id.ToString());
            }
            _itemId = id;
            _item = item;
            visitor.VisitItem(item);
        }

        private void ReadComponents()
        {
            reader.Expect(SyntaxStartComponents);
            visitor.VisitSuggestions(SuggestComponentAssignmentOrRemoval);
            while (reader.CanRead() && reader.Peek() != SyntaxEndComponents)
            {
                reader.SkipWhitespace();
                if (reader.CanRead() && reader.Peek() == SyntaxRemovedComponent)
                {
                    reader.Skip();
                    visitor.VisitSuggestions(SuggestComponent);
                    var removed = ReadComponentType(reader);
                    if (!_seen.Add(removed))
                        throw ErrorRepeatedComponent.Create(removed.ToString()!);
                    visitor.VisitRemovedComponent(removed);
                    visitor.VisitSuggestions(_ => { });
                    reader.SkipWhitespace();
                }
                else
                {
                    var type = ReadComponentType(reader);
                    if (!_seen.Add(type))
                        throw ErrorRepeatedComponent.Create(type.ToString()!);
                    visitor.VisitSuggestions(SuggestAssignment);
                    reader.SkipWhitespace();
                    reader.Expect(SyntaxComponentAssignment);
                    visitor.VisitSuggestions(_ => { });
                    reader.SkipWhitespace();
                    ReadComponent(ComponentTagParser, RegistryOpsForCommands, type);
                    reader.SkipWhitespace();
                }
                visitor.VisitSuggestions(SuggestNextOrEndComponents);
                if (!reader.CanRead() || reader.Peek() != SyntaxComponentSeparator)
                    break;
                reader.Skip();
                reader.SkipWhitespace();
                visitor.VisitSuggestions(SuggestComponentAssignmentOrRemoval);
                if (!reader.CanRead())
                    throw ErrorExpectedComponent.CreateWithContext(reader);
            }
            reader.Expect(SyntaxEndComponents);
            visitor.VisitSuggestions(_ => { });
        }

        //ReadComponentType reads the component identifier and looks it up in the registry; a non-persistent component is reported as unknown like vanilla
        private static DataComponentType<object> ReadComponentType(StringReader reader)
        {
            if (!reader.CanRead())
                throw ErrorExpectedComponent.CreateWithContext(reader);
            var cursor = reader.Cursor;
            var id = IdentifierArgument.ReadIdentifier(reader);
            if (BuiltInRegistries.DATA_COMPONENT_TYPE.GetValue(id) is not DataComponentType<object> type
                || type.IsTransient)
            {
                reader.SetCursor(cursor);
                throw ErrorUnknownComponent.CreateWithContext(reader, id.ToString());
            }
            return type;
        }

        //ReadComponent reads the value as SNBT into a Tag then hands it to the component Codec
        private void ReadComponent(TagParser<Tag> tagParser, RegistryOps<Tag> registryOps, DataComponentType<object> type)
        {
            var cursor = reader.Cursor;
            //The SNBT parser uses a separate reader and syncs the cursor after parsing
            var nbtReader = new CommandStringReader(reader.String) { Cursor = reader.Cursor };
            var tag = tagParser.ParseAsArgument(nbtReader);
            reader.SetCursor(nbtReader.Cursor);
            var parsed = type.CodecOrThrow().Parse(registryOps, tag);
            var value = parsed.MapOrElse<object>(
                result => result,
                message =>
                {
                    reader.SetCursor(cursor);
                    throw ErrorMalformedComponent.CreateWithContext(reader, type.ToString()!, message);
                });
            _components[type] = Optional<object>.Of(value);
            visitor.VisitComponent(type, value);
        }

        private static void SuggestStartComponents(SuggestionsBuilder builder)
        {
            if (builder.Remaining.Length == 0)
                builder.Add(SyntaxStartComponents.ToString());
        }

        private static void SuggestNextOrEndComponents(SuggestionsBuilder builder)
        {
            if (builder.Remaining.Length == 0)
            {
                builder.Add(SyntaxComponentSeparator.ToString());
                builder.Add(SyntaxEndComponents.ToString());
            }
        }

        private static void SuggestAssignment(SuggestionsBuilder builder)
        {
            if (builder.Remaining.Length == 0)
                builder.Add(SyntaxComponentAssignment.ToString());
        }

        private static void SuggestItem(SuggestionsBuilder builder)
        {
            foreach (var id in BuiltInRegistries.ITEM.KeySet)
            {
                var text = id.ToShortString();
                if (text.StartsWith(builder.RemainingLowerCase, StringComparison.Ordinal))
                    builder.Add(text);
            }
        }

        private static void SuggestComponentAssignmentOrRemoval(SuggestionsBuilder builder)
        {
            builder.Add(SyntaxRemovedComponent.ToString());
            SuggestComponent(builder, SyntaxComponentAssignment.ToString());
        }

        private static void SuggestComponent(SuggestionsBuilder builder)
            => SuggestComponent(builder, string.Empty);

        private static void SuggestComponent(SuggestionsBuilder builder, string suffix)
        {
            var contents = builder.Remaining.ToLowerInvariant();
            foreach (var id in BuiltInRegistries.DATA_COMPONENT_TYPE.KeySet)
            {
                if (BuiltInRegistries.DATA_COMPONENT_TYPE.GetValue(id) is not DataComponentType<object> type
                    || type.Codec is null)
                    continue;
                var text = id.ToString();
                if (text.StartsWith(contents, StringComparison.Ordinal))
                    builder.Add(text + suffix);
            }
        }
    }

    //SuggestionsVisitor keeps the last suggestion function the walk published
    private sealed class SuggestionsVisitor : Visitor
    {
        private Action<SuggestionsBuilder> _suggestions = _ => { };

        public void VisitSuggestions(Action<SuggestionsBuilder> suggestions) => _suggestions = suggestions;

        public void Suggest(SuggestionsBuilder builder) => _suggestions(builder);
    }
}
