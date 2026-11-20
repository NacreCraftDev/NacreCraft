using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Gui.Font;

//ActiveArea the clickable area of one text run, maps to vanilla net.minecraft.client.gui.font.ActiveArea
public interface ActiveArea
{
    Style Style { get; }

    float ActiveLeft { get; }

    float ActiveTop { get; }

    float ActiveRight { get; }

    float ActiveBottom { get; }
}
