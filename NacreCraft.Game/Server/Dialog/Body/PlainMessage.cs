using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using RegistryDialogBody = NacreCraft.Registry.DialogBody;

namespace NacreCraft.Game.Server.Dialog;

//PlainMessage a text paragraph in a dialog, maps to vanilla net.minecraft.server.dialog.body.PlainMessage
public sealed record PlainMessage(Component Contents, int Width) : DialogBody
{
    public const int DEFAULT_WIDTH = 200;

    public static readonly MapCodec<PlainMessage> MAP_CODEC = RecordCodecBuilder.Of2(
        ComponentSerialization.JsonCodec.FieldOf("contents").ForGetter((PlainMessage message) => message.Contents),
        DialogCodecs.WidthCodec.OptionalFieldOf("width", DEFAULT_WIDTH).ForGetter((PlainMessage message) => message.Width),
        (contents, width) => new PlainMessage(contents, width));

    public static readonly Codec<PlainMessage> CODEC = Codecs.WithAlternative(
        Codecs.AsCodec(MAP_CODEC),
        ComponentSerialization.JsonCodec.ComapFlatMap(
            contents => DataResult<PlainMessage>.Success(new PlainMessage(contents, DEFAULT_WIDTH)),
            message => message.Contents));

    public static readonly MapCodec<RegistryDialogBody> ControlCodec =
        new XmapMapCodec<PlainMessage, RegistryDialogBody>(MAP_CODEC, value => value,
            body => (PlainMessage)body);

    public MapCodec<RegistryDialogBody> MapCodec() => ControlCodec;
}
