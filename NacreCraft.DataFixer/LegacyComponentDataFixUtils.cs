using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using NacreCraft.Codec;

namespace NacreCraft.DataFixer;

//Legacy component helpers, maps to vanilla net.minecraft.util.datafix.LegacyComponentDataFixUtils
//Old saves store chat components as plain strings or lenient JSON; these helpers normalise them to a {"text":...} payload
public static class LegacyComponentDataFixUtils
{
    private static readonly string EmptyContents = CreateTextComponentJson("");

    //LenientJsonOptions accepts the trailing commas and comments old payloads contain
    private static readonly JsonDocumentOptions LenientOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    //JsonOptions keeps non-ASCII text readable, matching the Gson output the vanilla helper produces
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static Dynamic<T> CreatePlainTextComponent<T>(DynamicOps<T> ops, string text)
        => new(ops, ops.CreateString(CreateTextComponentJson(text)));

    public static Dynamic<T> CreateEmptyComponent<T>(DynamicOps<T> ops)
        => new(ops, ops.CreateString(EmptyContents));

    public static string CreateTextComponentJson(string text)
        => new JsonObject { ["text"] = text }.ToJsonString(JsonOptions);

    public static string CreateTranslatableComponentJson(string key)
        => new JsonObject { ["translate"] = key }.ToJsonString(JsonOptions);

    public static Dynamic<T> CreateTranslatableComponent<T>(DynamicOps<T> ops, string key)
        => new(ops, ops.CreateString(CreateTranslatableComponentJson(key)));

    //RewriteFromLenient turns an old component string into a stable component JSON
    //A quoted string, object or array is parsed as JSON; anything else becomes a plain text component
    public static string RewriteFromLenient(string str)
    {
        if (string.IsNullOrEmpty(str) || str == "null") return EmptyContents;
        var firstChar = str[0];
        var lastChar = str[^1];
        if (firstChar == '"' && lastChar == '"' || firstChar == '{' && lastChar == '}' || firstChar == '[' && lastChar == ']')
        {
            try
            {
                var json = JsonNode.Parse(str, nodeOptions: null, LenientOptions);
                if (json is JsonValue value) return CreateTextComponentJson(ValueAsString(value));
                return json?.ToJsonString(JsonOptions) ?? EmptyContents;
            }
            catch (JsonException)
            {
                //Falls through to the plain text form, matching the vanilla catch
            }
        }
        return CreateTextComponentJson(str);
    }

    //IsStrictlyValidJson whether the component payload is valid strict JSON
    public static bool IsStrictlyValidJson<T>(Dynamic<T> component)
    {
        var text = component.AsString().Result();
        if (!text.IsPresent) return false;
        try
        {
            JsonDocument.Parse(text.Get());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    //ExtractTranslationString takes the translate key out of a component payload
    public static Optional<string> ExtractTranslationString(string component)
    {
        try
        {
            if (JsonNode.Parse(component, nodeOptions: null, LenientOptions) is JsonObject obj
                && obj["translate"] is JsonValue key)
                return Optional<string>.Of(ValueAsString(key));
        }
        catch (JsonException)
        {
            //Not a component payload
        }
        return Optional<string>.Empty();
    }

    //ValueAsString renders a JSON primitive the way Gson's getAsString does
    private static string ValueAsString(JsonValue value)
        => value.TryGetValue<string>(out var text) ? text : value.ToJsonString(JsonOptions);
}
