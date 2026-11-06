using System.Globalization;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Execution;
using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Commands.Functions;

//MacroFunction a function with macros, maps to vanilla net.minecraft.commands.functions.MacroFunction
//instantiate substitutes macro lines with the argument values and re-parses; results are cached per argument list, up to 8 entries
public class MacroFunction<T> : CommandFunction<T>
{
    //DecimalFormatString equivalent to vanilla DecimalFormat('#') with up to 15 fraction digits and no decimal point on integers
    private const string DecimalFormatString = "0.###############";

    //MaxCacheEntries instantiation cache limit
    private const int MaxCacheEntries = 8;

    private readonly List<string> _parameters;
    private readonly LinkedList<KeyValuePair<List<string>, InstantiatedFunction<T>>> _cache = new();
    private readonly Identifier _id;
    private readonly List<Entry<T>> _entries;

    public MacroFunction(Identifier id, List<Entry<T>> entries, List<string> parameters)
    {
        _id = id;
        _entries = entries;
        _parameters = parameters;
    }

    public Identifier Id => _id;

    //Instantiate instantiates with arguments and blows up on missing ones; maps to vanilla instantiate
    public InstantiatedFunction<T> Instantiate(CompoundTag? arguments, CommandDispatcher<T> dispatcher)
    {
        if (arguments is null)
        {
            throw new FunctionInstantiationException($"Function {_id} requires macro arguments but none were provided");
        }
        var parameterValues = new List<string>(_parameters.Count);
        foreach (var parameter in _parameters)
        {
            var argumentValue = arguments[parameter];
            if (argumentValue is null)
            {
                throw new FunctionInstantiationException($"Function {_id} is missing argument '{parameter}'");
            }
            parameterValues.Add(Stringify(argumentValue));
        }
        if (GetCached(parameterValues) is { } cachedFunction)
        {
            return cachedFunction;
        }
        if (_cache.Count >= MaxCacheEntries)
        {
            _cache.RemoveFirst();
        }
        var function = SubstituteAndParse(parameterValues, dispatcher);
        _cache.AddLast(new KeyValuePair<List<string>, InstantiatedFunction<T>>(parameterValues, function));
        return function;
    }

    //GetCached looks up the cache and moves a hit to the tail
    private InstantiatedFunction<T>? GetCached(List<string> parameterValues)
    {
        foreach (var pair in _cache)
        {
            if (pair.Key.SequenceEqual(parameterValues))
            {
                _cache.Remove(pair);
                _cache.AddLast(pair);
                return pair.Value;
            }
        }
        return null;
    }

    //Stringify converts an argument value to text; floating points use DECIMAL_FORMAT; maps to vanilla stringify
    private static string Stringify(Tag tag)
    {
        switch (tag)
        {
            case FloatTag floatTag:
                return FormatJavaDecimal(floatTag.Value);
            case DoubleTag doubleTag:
                return FormatJavaDecimal(doubleTag.Value);
            case ByteTag byteTag:
                return byteTag.Value.ToString();
            case ShortTag shortTag:
                return shortTag.Value.ToString();
            case LongTag longTag:
                return longTag.Value.ToString();
            case StringTag stringTag:
                return stringTag.Value;
            default:
                return tag.ToString();
        }
    }

    //FormatJavaDecimal formats with the vanilla DecimalFormat("#") pattern: at most 15 fraction digits, trailing zeros dropped,
    //and a fractional value in (-1,1) loses its leading zero (".5" rather than "0.5"), which is what the Java formatter does
    private static string FormatJavaDecimal(double value)
    {
        if (value == 0.0) return "0";
        var text = value.ToString(DecimalFormatString, CultureInfo.InvariantCulture);
        if (text.StartsWith("0.", StringComparison.Ordinal)) return text[1..];
        if (text.StartsWith("-0.", StringComparison.Ordinal)) return "-" + text[2..];
        return text;
    }

    //ListHash the Java List.hashCode of the parameter values, which vanilla uses to name the instantiated function
    //A C# List has reference equality, so the content hash has to be computed explicitly
    private static int ListHash(List<string> values)
    {
        var hash = 1;
        foreach (var value in values)
        {
            hash = unchecked(31 * hash + StringHash(value));
        }
        return hash;
    }

    //StringHash the Java String.hashCode of one value
    private static int StringHash(string value)
    {
        var hash = 0;
        foreach (var c in value)
        {
            hash = unchecked(31 * hash + c);
        }
        return hash;
    }

    private InstantiatedFunction<T> SubstituteAndParse(List<string> values, CommandDispatcher<T> dispatcher)
    {
        var newEntries = new List<UnboundEntryAction<T>>(_entries.Count);
        var entryArguments = new List<string>(values.Count);
        foreach (var entry in _entries)
        {
            LookupValues(values, entry.Parameters, entryArguments);
            newEntries.Add(entry.Instantiate(entryArguments, dispatcher, _id));
        }
        return new PlainTextFunction<T>(_id.WithPath(id => id + "/" + ListHash(values)), newEntries);
    }

    //LookupValues selects values by index, reusing the output list; maps to vanilla lookupValues
    private static void LookupValues(List<string> values, List<int> indicesToSelect, List<string> selectedValuesOutput)
    {
        selectedValuesOutput.Clear();
        foreach (var index in indicesToSelect)
        {
            selectedValuesOutput.Add(values[index]);
        }
    }

    //Entry function entry abstraction
    public interface Entry<T2>
    {
        //Parameters indices of the referenced macro arguments
        List<int> Parameters { get; }

        //UnboundEntryAction instantiates an action from the arguments
        UnboundEntryAction<T2> Instantiate(List<string> substitutions, CommandDispatcher<T2> dispatcher, Identifier functionId);
    }

    //MacroEntry macro line entry
    public sealed class MacroEntry<T2>(
        StringTemplate template, List<int> parameters, T2 compilationContext) : Entry<T2>
    {
        public List<int> Parameters => parameters;

        public UnboundEntryAction<T2> Instantiate(List<string> substitutions, CommandDispatcher<T2> dispatcher, Identifier functionId)
        {
            var command = template.Substitute(substitutions);
            try
            {
                return CommandFunction<T2>.ParseCommand(dispatcher, compilationContext, new StringReader(command));
            }
            catch (CommandSyntaxException e)
            {
                throw new FunctionInstantiationException($"Failed to parse function {functionId} on command '{command}': {e.Message}");
            }
        }
    }

    //PlainTextEntry already compiled plain entry
    public sealed class PlainTextEntry<T2>(UnboundEntryAction<T2> compiledAction) : Entry<T2>
    {
        public List<int> Parameters => [];

        public UnboundEntryAction<T2> Instantiate(List<string> substitutions, CommandDispatcher<T2> dispatcher, Identifier functionId)
            => compiledAction;
    }
}
