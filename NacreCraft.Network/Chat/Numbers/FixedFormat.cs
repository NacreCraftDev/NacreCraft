namespace NacreCraft.Network.Chat.Numbers;

//Fixed number format, maps to vanilla FixedFormat: every score shows the same fixed text
public sealed class FixedFormat(Component value) : NumberFormat
{
    public Component Value { get; } = value;

    public int TypeId => NumberFormatTypes.FixedTypeId;

    //Vanilla returns a copy so a caller mutating the result cannot change the shared text
    public Component Format(int value) => Value is MutableComponent mutable ? mutable.Copy() : Value;
}
