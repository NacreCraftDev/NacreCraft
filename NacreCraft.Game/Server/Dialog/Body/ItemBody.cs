using NacreCraft.Codec;
using NacreCraft.Game.World.Items;
using NacreCraft.Util;
using RegistryDialogBody = NacreCraft.Registry.DialogBody;

namespace NacreCraft.Game.Server.Dialog;

//ItemBody an item icon with optional description, maps to vanilla net.minecraft.server.dialog.body.ItemBody
public sealed record ItemBody(ItemStackTemplate Item, Optional<PlainMessage> Description, bool ShowDecorations,
    bool ShowTooltip, int Width, int Height) : DialogBody
{
    public static readonly MapCodec<ItemBody> MAP_CODEC = RecordCodecBuilder.Of6(
        ItemStackTemplate.PersistentCodec.FieldOf("item").ForGetter((ItemBody body) => body.Item),
        PlainMessage.CODEC.OptionalFieldOf("description").ForGetter((ItemBody body) => body.Description),
        Codecs.Bool.OptionalFieldOf("show_decorations", true).ForGetter((ItemBody body) => body.ShowDecorations),
        Codecs.Bool.OptionalFieldOf("show_tooltip", true).ForGetter((ItemBody body) => body.ShowTooltip),
        ExtraCodecs.IntRange(1, 256).OptionalFieldOf("width", 16).ForGetter((ItemBody body) => body.Width),
        ExtraCodecs.IntRange(1, 256).OptionalFieldOf("height", 16).ForGetter((ItemBody body) => body.Height),
        (item, description, showDecorations, showTooltip, width, height) =>
            new ItemBody(item, description, showDecorations, showTooltip, width, height));

    public static readonly MapCodec<RegistryDialogBody> ControlCodec =
        new XmapMapCodec<ItemBody, RegistryDialogBody>(MAP_CODEC, value => value, body => (ItemBody)body);

    public MapCodec<RegistryDialogBody> MapCodec() => ControlCodec;
}
