namespace NacreCraft.Network.Chat.Numbers;

//Styled number format, maps to vanilla StyledFormat: the decimal value rendered with a style
public sealed class StyledFormat(Style style) : NumberFormat
{
    public static readonly StyledFormat NoStyle = new(Style.Empty);
    public static readonly StyledFormat SidebarDefault = new(Style.Empty.WithColor(ChatFormatting.Red));
    public static readonly StyledFormat PlayerListDefault = new(Style.Empty.WithColor(ChatFormatting.Yellow));

    public Style Style { get; } = style;

    public int TypeId => NumberFormatTypes.StyledTypeId;

    public Component Format(int value) => Component.Literal(value.ToString()).WithStyle(Style);
}
