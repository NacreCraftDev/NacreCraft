using System.Text.Json;

namespace NacreCraft.Codec;

//JsonValueRef one JSON value, either a JsonElement or a bare string used as a map key
//JsonElement cannot represent a standalone property name, so map keys carry the string form while values carry the element
public readonly struct JsonValueRef
{
    private readonly JsonElement _element;
    private readonly string? _string;

    public JsonValueRef(JsonElement element)
    {
        _element = element;
        _string = null;
    }

    public JsonValueRef(string value)
    {
        _element = default;
        _string = value;
    }

    public bool IsStringRef => _string is not null;

    public JsonElement Element => _element;

    public string StringRef => _string!;
}

//JsonElementOps read-only DynamicOps backed by JsonElement, used by the data-driven loading path
//JsonDocument owns the parsed buffer and must outlive every value read from it, the loader disposes it after decoding
//Write methods are not supported: loading only decodes, everything that encodes goes through JsonOps or NbtOps
public sealed class JsonElementOps : DynamicOps<JsonValueRef>
{
    public static readonly JsonElementOps Instance = new();

    private JsonElementOps() { }

    //Parse reads the whole stream into a document, the caller owns it and must dispose
    public static DataResult<JsonDocument> Parse(Stream stream)
    {
        try
        {
            return DataResult<JsonDocument>.Success(JsonDocument.Parse(stream));
        }
        catch (JsonException ex)
        {
            return DataResult<JsonDocument>.Error(() => $"JSON parse failed: {ex.Message}");
        }
    }

    public JsonValueRef Empty() => default;

    public JsonValueRef EmptyList() => default;

    public JsonValueRef EmptyMap() => default;

    public JsonValueRef CreateByte(byte value) => throw ReadOnly();

    public JsonValueRef CreateShort(short value) => throw ReadOnly();

    public JsonValueRef CreateInt(int value) => throw ReadOnly();

    public JsonValueRef CreateLong(long value) => throw ReadOnly();

    public JsonValueRef CreateFloat(float value) => throw ReadOnly();

    public JsonValueRef CreateDouble(double value) => throw ReadOnly();

    public JsonValueRef CreateBoolean(bool value) => throw ReadOnly();

    public JsonValueRef CreateNumeric(double value) => throw ReadOnly();

    //CreateString is the one write that has a representation, map keys and EncodeId go through it
    public JsonValueRef CreateString(string value) => new(value);

    public JsonValueRef CreateList(IEnumerable<JsonValueRef> stream) => throw ReadOnly();

    public JsonValueRef CreateMap(IEnumerable<Pair<JsonValueRef, JsonValueRef>> map) => throw ReadOnly();

    public DataResult<double> GetNumberValue(JsonValueRef input)
    {
        if (!input.IsStringRef && input.Element.ValueKind == JsonValueKind.Number && input.Element.TryGetDouble(out var value))
            return DataResult<double>.Success(value);
        return DataResult<double>.Error(() => $"Not a number: {Describe(input)}");
    }

    public DataResult<long> GetLongValue(JsonValueRef input)
    {
        if (!input.IsStringRef && input.Element.ValueKind == JsonValueKind.Number && input.Element.TryGetInt64(out var value))
            return DataResult<long>.Success(value);
        return DataResult<long>.Error(() => $"Not a long: {Describe(input)}");
    }

    public DataResult<string> GetStringValue(JsonValueRef input)
    {
        if (input.IsStringRef) return DataResult<string>.Success(input.StringRef);
        if (input.Element.ValueKind == JsonValueKind.String) return DataResult<string>.Success(input.Element.GetString()!);
        return DataResult<string>.Error(() => $"Not a string: {Describe(input)}");
    }

    public DataResult<bool> GetBooleanValue(JsonValueRef input)
    {
        if (!input.IsStringRef && input.Element.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return DataResult<bool>.Success(input.Element.ValueKind == JsonValueKind.True);
        return DataResult<bool>.Error(() => $"Not a boolean: {Describe(input)}");
    }

    public DataResult<JsonValueRef> MergeToList(JsonValueRef list, JsonValueRef value) => throw ReadOnly();

    public DataResult<JsonValueRef> MergeToList(JsonValueRef list, IReadOnlyList<JsonValueRef> values) => throw ReadOnly();

    public DataResult<JsonValueRef> MergeToMap(JsonValueRef map, JsonValueRef key, JsonValueRef value) => throw ReadOnly();

    public DataResult<JsonValueRef> MergeToMap(JsonValueRef map, MapLike<JsonValueRef> values) => throw ReadOnly();

    public DataResult<JsonValueRef> MergeToMap(JsonValueRef map, IReadOnlyDictionary<JsonValueRef, JsonValueRef> values) => throw ReadOnly();

    public DataResult<MapLike<JsonValueRef>> GetMap(JsonValueRef input)
    {
        if (!input.IsStringRef && input.Element.ValueKind == JsonValueKind.Object)
            return DataResult<MapLike<JsonValueRef>>.Success(new JsonElementMapLike(input.Element));
        return DataResult<MapLike<JsonValueRef>>.Error(() => $"Not an object: {Describe(input)}");
    }

    public DataResult<IEnumerable<Pair<JsonValueRef, JsonValueRef>>> GetMapValues(JsonValueRef input)
    {
        if (input.IsStringRef || input.Element.ValueKind != JsonValueKind.Object)
            return DataResult<IEnumerable<Pair<JsonValueRef, JsonValueRef>>>.Error(() => $"Not an object: {Describe(input)}");
        return DataResult<IEnumerable<Pair<JsonValueRef, JsonValueRef>>>.Success(
            input.Element.EnumerateObject()
                .Select(p => new Pair<JsonValueRef, JsonValueRef>(new JsonValueRef(p.Name), new JsonValueRef(p.Value)))
                .ToList());
    }

    public DataResult<IEnumerable<JsonValueRef>> GetStream(JsonValueRef input)
    {
        if (input.IsStringRef || input.Element.ValueKind != JsonValueKind.Array)
            return DataResult<IEnumerable<JsonValueRef>>.Error(() => $"Not an array: {Describe(input)}");
        return DataResult<IEnumerable<JsonValueRef>>.Success(
            input.Element.EnumerateArray().Select(e => new JsonValueRef(e)).ToList());
    }

    public JsonValueRef Remove(JsonValueRef input, string key) => input;

    public U ConvertTo<U>(DynamicOps<U> ops, JsonValueRef input)
    {
        if (input.IsStringRef) return ops.CreateString(input.StringRef);
        var element = input.Element;
        switch (element.ValueKind)
        {
            case JsonValueKind.Null or JsonValueKind.Undefined:
                return ops.Empty();
            case JsonValueKind.Object:
                return ops.CreateMap(element.EnumerateObject()
                    .Select(p => new Pair<U, U>(ops.CreateString(p.Name), ConvertTo(ops, new JsonValueRef(p.Value)))));
            case JsonValueKind.Array:
                return ops.CreateList(element.EnumerateArray().Select(e => ConvertTo(ops, new JsonValueRef(e))));
            case JsonValueKind.String:
                return ops.CreateString(element.GetString()!);
            case JsonValueKind.True:
                return ops.CreateBoolean(true);
            case JsonValueKind.False:
                return ops.CreateBoolean(false);
            case JsonValueKind.Number:
                return element.TryGetInt64(out var longValue)
                    ? ops.CreateLong(longValue)
                    : ops.CreateDouble(element.GetDouble());
            default:
                throw new InvalidOperationException($"Cannot convert JSON value: {Describe(input)}");
        }
    }

    public RecordBuilder<JsonValueRef> MapBuilder() => throw ReadOnly();

    private static NotSupportedException ReadOnly() => new("JsonElementOps is read-only");

    private static string Describe(JsonValueRef input)
        => input.IsStringRef
            ? $"\"{input.StringRef}\""
            : input.Element.ValueKind == JsonValueKind.Undefined ? "undefined" : input.Element.GetRawText();

    //JsonElementMapLike map view over a JsonObject, keys are the string form and values are elements
    private sealed class JsonElementMapLike(JsonElement value) : MapLike<JsonValueRef>
    {
        public Optional<JsonValueRef> Get(JsonValueRef key)
        {
            if (key.IsStringRef) return Get(key.StringRef);
            if (key.Element.ValueKind == JsonValueKind.String) return Get(key.Element.GetString()!);
            return Optional<JsonValueRef>.Empty();
        }

        public Optional<JsonValueRef> Get(string key)
            => value.TryGetProperty(key, out var child)
                ? Optional<JsonValueRef>.Of(new JsonValueRef(child))
                : Optional<JsonValueRef>.Empty();

        public IEnumerable<Pair<JsonValueRef, JsonValueRef>> Entries()
            => value.EnumerateObject()
                .Select(p => new Pair<JsonValueRef, JsonValueRef>(new JsonValueRef(p.Name), new JsonValueRef(p.Value)));
    }
}
