using NacreCraft.Codec;

namespace NacreCraft.Game.Server.Dialog;

//Input one dialog input binding, maps to vanilla net.minecraft.server.dialog.Input
public sealed record Input(string Key, InputControl Control)
{
    public static readonly Codec<Input> CODEC = RecordCodecBuilder.Of2(
        ParsedTemplate.VARIABLE_CODEC.FieldOf("key").ForGetter((Input input) => input.Key),
        InputControls.MapCodec.ForGetter((Input input) => input.Control),
        (key, control) => new Input(key, control));
}
