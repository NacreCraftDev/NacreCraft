using NacreCraft.Commands.Arguments;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//RangeArgument min..max range argument, maps to vanilla net.minecraft.commands.arguments.RangeArgument
//Parsing is delegated to MinMaxBounds; the int and double variants share this interface
public interface RangeArgument<T> : ArgumentType<T>
{
    public static Ints IntRange() => new();

    public static Floats FloatRange() => new();

    public sealed class Ints : RangeArgument<MinMaxBounds.Ints>
    {
        private static readonly IReadOnlyList<string> ExampleList = new[] { "0..5", "0", "-5", "-100..", "..100" };

        public MinMaxBounds.Ints Parse(StringReader reader) => MinMaxBounds.Ints.FromReader(reader);

        public IReadOnlyList<string> Examples => ExampleList;
    }

    public sealed class Floats : RangeArgument<MinMaxBounds.Doubles>
    {
        private static readonly IReadOnlyList<string> ExampleList =
            new[] { "0..5.2", "0", "-5.4", "-100.76..", "..100" };

        public MinMaxBounds.Doubles Parse(StringReader reader) => MinMaxBounds.Doubles.FromReader(reader);

        public IReadOnlyList<string> Examples => ExampleList;
    }
}
