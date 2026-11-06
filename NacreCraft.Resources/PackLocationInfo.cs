using NacreCraft.Codec;
using NacreCraft.Network.Chat;

namespace NacreCraft.Resources;

//PackLocationInfo where a pack lives and what to show for it, maps to vanilla net.minecraft.server.packs.PackLocationInfo
public sealed record PackLocationInfo(string Id, Component Title, PackSource Source, Optional<KnownPack> KnownPackInfo)
{
    //CreateChatLink the clickable pack name used in chat, maps to vanilla createChatLink
    public Component CreateChatLink(bool enabled, Component description)
        => ComponentUtils.WrapInSquareBrackets(Component.Literal(Id)).WithStyle(style => style
            .WithColor(enabled ? ChatFormatting.Green : ChatFormatting.Red)
            .WithInsertion(Id)
            .WithHoverEvent(new HoverEvent.ShowText(
                Component.Empty().Append(Title).Append("\n").Append(description))));
}
