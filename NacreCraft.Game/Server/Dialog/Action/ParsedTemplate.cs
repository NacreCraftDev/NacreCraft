using NacreCraft.Codec;
using NacreCraft.Commands.Functions;

namespace NacreCraft.Game.Server.Dialog;

//ParsedTemplate a macro command template, maps to vanilla net.minecraft.server.dialog.action.ParsedTemplate
public sealed class ParsedTemplate
{
    public static readonly Codec<ParsedTemplate> CODEC =
        Codecs.String.ComapFlatMap(Parse, template => template._raw);

    public static readonly Codec<string> VARIABLE_CODEC = Codecs.String.ComapFlatMap(
        value => StringTemplate.IsValidVariableName(value)
            ? DataResult<string>.Success(value)
            : DataResult<string>.Error(() => $"{value} is not a valid input name"),
        value => value);

    private readonly string _raw;
    private readonly StringTemplate _parsed;

    private ParsedTemplate(string raw, StringTemplate parsed)
    {
        _raw = raw;
        _parsed = parsed;
    }

    private static DataResult<ParsedTemplate> Parse(string value)
    {
        try
        {
            return DataResult<ParsedTemplate>.Success(new ParsedTemplate(value, StringTemplate.FromString(value)));
        }
        catch (Exception e)
        {
            return DataResult<ParsedTemplate>.Error(() => $"Failed to parse template {value}: {e.Message}");
        }
    }

    //Instantiate fills the variables with the given arguments, maps to vanilla instantiate
    public string Instantiate(IReadOnlyDictionary<string, string> arguments)
    {
        var values = _parsed.Variables.Select(name => arguments.GetValueOrDefault(name, "")).ToList();
        return _parsed.Substitute(values);
    }
}
