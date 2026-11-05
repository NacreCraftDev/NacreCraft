using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Network.Chat;
using NacreCraft.Util;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//AngleArgument yaw angle, maps to vanilla net.minecraft.commands.arguments.AngleArgument
//A leading ~ makes the angle relative to the source rotation
public sealed class AngleArgument : ArgumentType<AngleArgument.SingleAngle>
{
    public static readonly SimpleCommandExceptionType ErrorNotComplete =
        new(new TranslatableMessage("argument.angle.incomplete"));

    public static readonly SimpleCommandExceptionType ErrorInvalidAngle =
        new(new TranslatableMessage("argument.angle.invalid"));

    private static readonly IReadOnlyList<string> ExampleList = new[] { "0", "~", "~-5" };

    public static AngleArgument Angle() => new();

    public SingleAngle Parse(StringReader reader)
    {
        if (!reader.CanRead()) throw ErrorNotComplete.CreateWithContext(reader);
        var isRelative = reader.Peek() == '~';
        if (isRelative) reader.Skip();
        var value = reader.CanRead() && reader.Peek() != ' ' ? reader.ReadFloat() : 0f;
        if (float.IsNaN(value) || float.IsInfinity(value)) throw ErrorInvalidAngle.CreateWithContext(reader);
        return new SingleAngle(value, isRelative);
    }

    public IReadOnlyList<string> Examples => ExampleList;

    //SingleAngle parsed angle with the relative flag, maps to vanilla SingleAngle
    public sealed record SingleAngle(float Angle, bool IsRelative)
    {
        //GetAngle resolves the absolute angle against the source yaw
        public float GetAngle(float sourceYRotation)
            => Mth.WrapDegrees(IsRelative ? Angle + sourceYRotation : Angle);
    }
}
