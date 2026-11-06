using NacreCraft.Codec;
using NacreCraft.Network.Chat;

namespace NacreCraft.Game.Server.Dialog;

//CommonButtonData the label, tooltip and width of one dialog button, maps to vanilla net.minecraft.server.dialog.CommonButtonData
public sealed record CommonButtonData(Component Label, Optional<Component> Tooltip, int Width)
{
    public const int DEFAULT_WIDTH = 150;

    public static readonly MapCodec<CommonButtonData> MAP_CODEC = RecordCodecBuilder.Of3(
        ComponentSerialization.JsonCodec.FieldOf("label").ForGetter((CommonButtonData data) => data.Label),
        ComponentSerialization.JsonCodec.OptionalFieldOf("tooltip").ForGetter((CommonButtonData data) => data.Tooltip),
        DialogCodecs.WidthCodec.OptionalFieldOf("width", DEFAULT_WIDTH).ForGetter((CommonButtonData data) => data.Width),
        (label, tooltip, width) => new CommonButtonData(label, tooltip, width));

    public CommonButtonData(Component label, int width) : this(label, Optional<Component>.Empty(), width)
    {
    }
}
