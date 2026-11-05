using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using RegistryInputControl = NacreCraft.Registry.InputControl;

namespace NacreCraft.Game.Server.Dialog;

//BooleanInput a checkbox input, maps to vanilla net.minecraft.server.dialog.input.BooleanInput
public sealed record BooleanInput(Component Label, bool Initial, string OnTrue, string OnFalse) : InputControl
{
    public static readonly MapCodec<BooleanInput> MAP_CODEC = RecordCodecBuilder.Of4(
        ComponentSerialization.JsonCodec.FieldOf("label").ForGetter((BooleanInput input) => input.Label),
        Codecs.Bool.OptionalFieldOf("initial", false).ForGetter((BooleanInput input) => input.Initial),
        Codecs.String.OptionalFieldOf("on_true", "true").ForGetter((BooleanInput input) => input.OnTrue),
        Codecs.String.OptionalFieldOf("on_false", "false").ForGetter((BooleanInput input) => input.OnFalse),
        (label, initial, onTrue, onFalse) => new BooleanInput(label, initial, onTrue, onFalse));

    public static readonly MapCodec<RegistryInputControl> ControlCodec =
        new XmapMapCodec<BooleanInput, RegistryInputControl>(MAP_CODEC, value => value,
            control => (BooleanInput)control);

    public MapCodec<RegistryInputControl> MapCodec() => ControlCodec;
}
