using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Gui.Font;

//EmptyArea an active area with no glyph behind it, maps to vanilla net.minecraft.client.gui.font.EmptyArea
public sealed record EmptyArea(float X, float Y, float Advance, float Ascent, float Height, Style Style) : ActiveArea
{
    public const float DefaultHeight = 9.0f;
    public const float DefaultAscent = 7.0f;

    public float ActiveLeft => X;

    public float ActiveTop => Y + 7.0f - Ascent;

    public float ActiveRight => X + Advance;

    public float ActiveBottom => ActiveTop + Height;
}
