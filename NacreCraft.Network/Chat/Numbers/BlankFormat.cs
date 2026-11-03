namespace NacreCraft.Network.Chat.Numbers;

//Blank number format, maps to vanilla BlankFormat: the score value is never shown
public sealed class BlankFormat : NumberFormat
{
    public static readonly BlankFormat Instance = new();

    private BlankFormat() { }

    public int TypeId => NumberFormatTypes.BlankTypeId;

    public Component Format(int value) => Component.Empty();
}
