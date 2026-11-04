using System.Globalization;

using NacreCraft.Commands;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//BlockStateParser shared parser of the block argument syntax, maps to vanilla net.minecraft.commands.arguments.blocks.BlockStateParser
//Syntax: <block> | #<tag>, then an optional [property=value,...] section and an optional {block entity nbt} section
//Whitespace is skipped around every token, matching the vanilla char-by-char reader instead of the token splitting the arguments used before
public sealed class BlockStateParser
{
    public static readonly SimpleCommandExceptionType ErrorNoTagsAllowed =
        new(new LiteralMessage("Block tags are not allowed here"));

    public static readonly DynamicCommandExceptionType ErrorUnknownBlock =
        new(id => new LiteralMessage($"unknown block {id}"));

    public static readonly Dynamic2CommandExceptionType ErrorUnknownProperty =
        new((block, property) => new LiteralMessage($"block {block} does not have the property {property}"));

    public static readonly Dynamic2CommandExceptionType ErrorDuplicateProperty =
        new((block, property) => new LiteralMessage($"property {property} was specified twice for block {block}"));

    public static readonly Dynamic3CommandExceptionType ErrorInvalidValue =
        new((block, property, value) => new LiteralMessage($"invalid value {value} for property {property} of block {block}"));

    public static readonly Dynamic2CommandExceptionType ErrorExpectedValue =
        new((block, property) => new LiteralMessage($"expected value for property {property} of block {block}"));

    public static readonly SimpleCommandExceptionType ErrorExpectedEndOfProperties =
        new(new LiteralMessage("expected end of properties"));

    public static readonly DynamicCommandExceptionType ErrorUnknownTag =
        new(tag => new LiteralMessage($"unknown block tag {tag}"));

    private const char SyntaxStartProperties = '[';
    private const char SyntaxStartNbt = '{';
    private const char SyntaxEndProperties = ']';
    private const char SyntaxEquals = '=';
    private const char SyntaxPropertySeparator = ',';
    private const char SyntaxTag = '#';

    private readonly StringReader _reader;
    private readonly bool _forTesting;
    private readonly bool _allowNbt;
    private readonly Dictionary<PropertyBase, object> _properties = new();
    private readonly Dictionary<string, string> _vagueProperties = new();

    private Identifier _id = Identifier.WithDefaultNamespace("");
    private Block? _block;
    private BlockState _state;
    private CompoundTag? _nbt;
    private HolderSet<NacreCraft.Registry.Block>? _tag;

    private BlockStateParser(StringReader reader, bool forTesting, bool allowNbt)
    {
        _reader = reader;
        _forTesting = forTesting;
        _allowNbt = allowNbt;
    }

    //ParseForBlock parses a block state with exact properties, maps to vanilla parseForBlock
    public static BlockResult ParseForBlock(string value, bool allowNbt) => ParseForBlock(new StringReader(value), allowNbt);

    public static BlockResult ParseForBlock(StringReader reader, bool allowNbt)
    {
        var cursor = reader.Cursor;
        try
        {
            var parser = new BlockStateParser(reader, forTesting: false, allowNbt);
            parser.Parse();
            return new BlockResult(parser._state, parser._properties, parser._nbt);
        }
        catch (CommandSyntaxException)
        {
            reader.SetCursor(cursor);
            throw;
        }
    }

    //ParseForTesting parses either a block state or a block tag with only the listed properties, maps to vanilla parseForTesting
    public static ParseResult ParseForTesting(string value, bool allowNbt) => ParseForTesting(new StringReader(value), allowNbt);

    public static ParseResult ParseForTesting(StringReader reader, bool allowNbt)
    {
        var cursor = reader.Cursor;
        try
        {
            var parser = new BlockStateParser(reader, forTesting: true, allowNbt);
            parser.Parse();
            return parser._tag is not null
                ? new ParseResult(null, new TagResult(parser._tag, parser._vagueProperties, parser._nbt))
                : new ParseResult(new BlockResult(parser._state, parser._properties, parser._nbt), null);
        }
        catch (CommandSyntaxException)
        {
            reader.SetCursor(cursor);
            throw;
        }
    }

    //FillSuggestions parses whatever is typed so far and suggests the next token, maps to vanilla fillSuggestions
    //The block entity section is never suggested; NC has no block to block-entity-type mapping to test for one
    public static Task<Suggestions> FillSuggestions(SuggestionsBuilder builder, bool forTesting, bool allowNbt)
    {
        var reader = new StringReader(builder.Input);
        reader.SetCursor(builder.Start);
        var parser = new BlockStateParser(reader, forTesting, allowNbt);
        try
        {
            parser.Parse();
        }
        catch (CommandSyntaxException)
        {
            //Suggestions ignore syntax errors and answer with the next token of the furthest position that did parse
        }
        return parser.Suggest(builder.CreateOffset(reader.Cursor));
    }

    private Task<Suggestions> Suggest(SuggestionsBuilder builder)
    {
        var remaining = builder.Remaining;
        if (_state.Id != 0 && remaining.Length == 0) return SuggestNextSection(builder);

        //Inside the property section the property name or the value of the current property is what still matches
        var sectionStart = _reader.String.LastIndexOf(SyntaxStartProperties, Math.Max(0, _reader.Cursor - 1));
        var equalsIndex = sectionStart >= 0 ? _reader.String.IndexOf(SyntaxEquals, sectionStart) : -1;
        var commaIndex = sectionStart >= 0 ? _reader.String.LastIndexOf(SyntaxPropertySeparator, Math.Max(0, _reader.Cursor - 1)) : -1;
        if (sectionStart >= 0 && equalsIndex > commaIndex && equalsIndex < _reader.Cursor)
        {
            SuggestPropertyValues(builder, _reader.String[(equalsIndex + 1).._reader.Cursor].Trim(), remaining);
            return builder.BuildFuture();
        }
        if (remaining.StartsWith(SyntaxTag))
        {
            SuggestTags(builder, remaining);
            return builder.BuildFuture();
        }
        if (_state.Id == 0)
        {
            SuggestBlocks(builder, remaining);
            if (_forTesting && SyntaxTag.ToString().StartsWith(remaining, StringComparison.Ordinal)) builder.Add("#");
            return builder.BuildFuture();
        }
        SuggestPropertyNames(builder, remaining);
        return builder.BuildFuture();
    }

    private Task<Suggestions> SuggestNextSection(SuggestionsBuilder builder)
    {
        if (_state.Id != 0 && _state.GetProperties().Count > 0) builder.Add("[");
        return builder.BuildFuture();
    }

    private static void SuggestBlocks(SuggestionsBuilder builder, string remaining)
    {
        foreach (var id in BuiltInRegistries.BLOCK.KeySet)
        {
            var text = id.ToShortString();
            if (text.StartsWith(remaining, StringComparison.Ordinal)) builder.Add(text);
        }
    }

    private static void SuggestTags(SuggestionsBuilder builder, string remaining)
    {
        foreach (var set in BuiltInRegistries.BLOCK.GetTags())
        {
            var text = "#" + set.Key.Location.ToShortString();
            if (text.StartsWith(remaining, StringComparison.Ordinal)) builder.Add(text);
        }
    }

    private void SuggestPropertyNames(SuggestionsBuilder builder, string remaining)
    {
        foreach (var property in _state.GetProperties())
        {
            if (_properties.ContainsKey(property)) continue;
            var text = property.Name + "=";
            if (text.StartsWith(remaining, StringComparison.OrdinalIgnoreCase)) builder.Add(text);
        }
    }

    private void SuggestPropertyValues(SuggestionsBuilder builder, string propertyName, string remaining)
    {
        foreach (var property in _state.GetProperties())
        {
            if (property.Name != propertyName) continue;
            foreach (var value in property.PossibleValuesAsObjects)
            {
                var text = property.GetNameForValue(value);
                if (text.StartsWith(remaining, StringComparison.OrdinalIgnoreCase)) builder.Add(text);
            }
            return;
        }
    }

    private void Parse()
    {
        if (_reader.CanRead() && _reader.Peek() == SyntaxTag)
        {
            ReadTag();
            if (_reader.CanRead() && _reader.Peek() == SyntaxStartProperties) ReadVagueProperties();
        }
        else
        {
            ReadBlock();
            if (_reader.CanRead() && _reader.Peek() == SyntaxStartProperties) ReadProperties();
        }
        if (_allowNbt && _reader.CanRead() && _reader.Peek() == SyntaxStartNbt) ReadNbt();
    }

    private void ReadBlock()
    {
        var start = _reader.Cursor;
        var id = ReadIdentifier(_reader);
        if (id is null || !BuiltInRegistries.BLOCK.ContainsKey(id.Value))
        {
            var text = _reader.String[start.._reader.Cursor];
            _reader.SetCursor(start);
            throw ErrorUnknownBlock.CreateWithContext(_reader, text);
        }
        _id = id.Value;
        _block = BuiltInRegistries.BLOCK.GetValue(_id)!;
        _state = _block.DefaultBlockState;
    }

    private void ReadTag()
    {
        if (!_forTesting) throw ErrorNoTagsAllowed.CreateWithContext(_reader);
        var start = _reader.Cursor;
        _reader.Expect(SyntaxTag);
        var id = ReadIdentifier(_reader);
        var key = id is null ? null : TagKey<NacreCraft.Registry.Block>.Create(Registries.BLOCK, id.Value);
        _tag = key is null ? null : BuiltInRegistries.BLOCK.Get(key);
        if (_tag is null)
        {
            var text = _reader.String[start.._reader.Cursor];
            _reader.SetCursor(start);
            throw ErrorUnknownTag.CreateWithContext(_reader, text);
        }
    }

    private void ReadProperties()
    {
        _reader.Skip();
        _reader.SkipWhitespace();
        while (_reader.CanRead() && _reader.Peek() != SyntaxEndProperties)
        {
            _reader.SkipWhitespace();
            var keyStart = _reader.Cursor;
            var key = _reader.ReadString();
            PropertyBase? property = null;
            foreach (var candidate in _state.GetProperties())
            {
                if (candidate.Name != key) continue;
                property = candidate;
                break;
            }
            if (property is null)
            {
                _reader.SetCursor(keyStart);
                throw ErrorUnknownProperty.CreateWithContext(_reader, _id.ToString(), key);
            }
            if (_properties.ContainsKey(property))
            {
                _reader.SetCursor(keyStart);
                throw ErrorDuplicateProperty.CreateWithContext(_reader, _id.ToString(), key);
            }
            _reader.SkipWhitespace();
            if (!_reader.CanRead() || _reader.Peek() != SyntaxEquals)
                throw ErrorExpectedValue.CreateWithContext(_reader, _id.ToString(), key);
            _reader.Skip();
            _reader.SkipWhitespace();
            var valueStart = _reader.Cursor;
            var value = _reader.ReadString();
            var parsed = property.GetValueForName(value);
            if (parsed is null)
            {
                _reader.SetCursor(valueStart);
                throw ErrorInvalidValue.CreateWithContext(_reader, _id.ToString(), property.Name, value);
            }
            _state = _state.SetValue(property, parsed);
            _properties[property] = parsed;
            _reader.SkipWhitespace();
            if (!_reader.CanRead()) continue;
            if (_reader.Peek() == SyntaxPropertySeparator)
            {
                _reader.Skip();
                continue;
            }
            if (_reader.Peek() == SyntaxEndProperties) break;
            throw ErrorExpectedEndOfProperties.CreateWithContext(_reader);
        }
        if (!_reader.CanRead()) throw ErrorExpectedEndOfProperties.CreateWithContext(_reader);
        _reader.Skip();
    }

    private void ReadVagueProperties()
    {
        _reader.Skip();
        _reader.SkipWhitespace();
        while (_reader.CanRead() && _reader.Peek() != SyntaxEndProperties)
        {
            _reader.SkipWhitespace();
            var keyStart = _reader.Cursor;
            var key = _reader.ReadString();
            if (_vagueProperties.ContainsKey(key))
            {
                _reader.SetCursor(keyStart);
                throw ErrorDuplicateProperty.CreateWithContext(_reader, _id.ToString(), key);
            }
            _reader.SkipWhitespace();
            if (!_reader.CanRead() || _reader.Peek() != SyntaxEquals)
            {
                _reader.SetCursor(keyStart);
                throw ErrorExpectedValue.CreateWithContext(_reader, _id.ToString(), key);
            }
            _reader.Skip();
            _reader.SkipWhitespace();
            _vagueProperties[key] = _reader.ReadString();
            _reader.SkipWhitespace();
            if (!_reader.CanRead()) continue;
            if (_reader.Peek() == SyntaxPropertySeparator)
            {
                _reader.Skip();
                continue;
            }
            if (_reader.Peek() == SyntaxEndProperties) break;
            throw ErrorExpectedEndOfProperties.CreateWithContext(_reader);
        }
        if (!_reader.CanRead()) throw ErrorExpectedEndOfProperties.CreateWithContext(_reader);
        _reader.Skip();
    }

    private void ReadNbt()
    {
        _nbt = TagParser<Tag>.ParseCompoundFully(_reader.Remaining);
        _reader.SetCursor(_reader.String.Length);
    }

    //ReadIdentifier reads a resource location straight from the reader, maps to vanilla Identifier.read
    //A bare path is defaulted to the minecraft namespace, matching the previous block argument behaviour
    private static Identifier? ReadIdentifier(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && IsAllowedInIdentifier(reader.Peek())) reader.Skip();
        var text = reader.String[start..reader.Cursor];
        if (text.Length == 0) return null;
        return text.Contains(':')
            ? Identifier.TryParse(text)
            : Identifier.TryParse("minecraft:" + text);
    }

    private static bool IsAllowedInIdentifier(char c)
        => c is (>= '0' and <= '9') or (>= 'a' and <= 'z') or '_' or ':' or '/' or '.' or '-';

    //Serialize writes a block state back into the argument syntax, maps to vanilla serialize
    public static string Serialize(BlockState state)
    {
        var id = BuiltInRegistries.BLOCK.GetKey(state.Owner);
        var result = new System.Text.StringBuilder(id?.ToString() ?? "minecraft:air");
        if (state.IsSingletonState) return result.ToString();
        result.Append(SyntaxStartProperties);
        var separate = false;
        foreach (var value in state.GetValues())
        {
            if (separate) result.Append(SyntaxPropertySeparator);
            result.Append(value.Property.Name).Append(SyntaxEquals).Append(value.ValueName);
            separate = true;
        }
        result.Append(SyntaxEndProperties);
        return result.ToString();
    }

    //BlockResult parsed block state plus the properties that were written explicitly, maps to vanilla BlockStateParser.BlockResult
    public sealed record BlockResult(
        BlockState BlockState,
        IReadOnlyDictionary<PropertyBase, object> Properties,
        CompoundTag? Nbt);

    //TagResult parsed block tag plus the vague properties, maps to vanilla BlockStateParser.TagResult
    public sealed record TagResult(
        HolderSet<NacreCraft.Registry.Block> Tag,
        IReadOnlyDictionary<string, string> VagueProperties,
        CompoundTag? Nbt);

    //ParseResult either a block or a tag result, maps to the Either the vanilla parseForTesting returns
    public sealed record ParseResult(BlockResult? Block, TagResult? Tag);
}
