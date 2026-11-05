using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Util;
using RegistryInputControl = NacreCraft.Registry.InputControl;

namespace NacreCraft.Game.Server.Dialog;

//TextInput a text field input, maps to vanilla net.minecraft.server.dialog.input.TextInput
public sealed record TextInput(int Width, Component Label, bool LabelVisible, string Initial, int MaxLength,
    Optional<TextInput.MultilineOptions> Multiline) : InputControl
{
    public static readonly MapCodec<TextInput> MAP_CODEC = RecordCodecBuilder.Of6(
        InputControls.WidthCodec.OptionalFieldOf("width", 200).ForGetter((TextInput input) => input.Width),
        ComponentSerialization.JsonCodec.FieldOf("label").ForGetter((TextInput input) => input.Label),
        Codecs.Bool.OptionalFieldOf("label_visible", true).ForGetter((TextInput input) => input.LabelVisible),
        Codecs.String.OptionalFieldOf("initial", "").ForGetter((TextInput input) => input.Initial),
        ExtraCodecs.PositiveInt.OptionalFieldOf("max_length", 32).ForGetter((TextInput input) => input.MaxLength),
        MultilineOptions.CODEC.OptionalFieldOf("multiline").ForGetter((TextInput input) => input.Multiline),
        (width, label, labelVisible, initial, maxLength, multiline) =>
            new TextInput(width, label, labelVisible, initial, maxLength, multiline))
        .ComapFlatMap(Validate, input => input);

    public static readonly MapCodec<RegistryInputControl> ControlCodec =
        new XmapMapCodec<TextInput, RegistryInputControl>(MAP_CODEC, value => value, control => (TextInput)control);

    public MapCodec<RegistryInputControl> MapCodec() => ControlCodec;

    private static DataResult<TextInput> Validate(TextInput input)
        => input.Initial.Length > input.MaxLength
            ? DataResult<TextInput>.Error(() => "Default text length exceeds allowed size")
            : DataResult<TextInput>.Success(input);

    public sealed record MultilineOptions(Optional<int> MaxLines, Optional<int> Height)
    {
        public const int MAX_HEIGHT = 512;

        public static readonly Codec<MultilineOptions> CODEC = RecordCodecBuilder.Of2(
            ExtraCodecs.PositiveInt.OptionalFieldOf("max_lines")
                .ForGetter((MultilineOptions options) => options.MaxLines),
            ExtraCodecs.IntRange(1, MAX_HEIGHT).OptionalFieldOf("height")
                .ForGetter((MultilineOptions options) => options.Height),
            (maxLines, height) => new MultilineOptions(maxLines, height));
    }
}
