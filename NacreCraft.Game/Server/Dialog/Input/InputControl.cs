using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Util;
using RegistryInputControl = NacreCraft.Registry.InputControl;

namespace NacreCraft.Game.Server.Dialog;

//InputControl one dialog input widget, maps to vanilla net.minecraft.server.dialog.input.InputControl
//The registry element type is the Registry layer's InputControl marker; implementations add their own codec
public interface InputControl : RegistryInputControl
{
    MapCodec<RegistryInputControl> MapCodec();
}

//InputControls shared codecs of the input package, maps to the static members of vanilla InputControl/Dialog
public static class InputControls
{
    //WidthCodec the allowed dialog input width, maps to vanilla Dialog.WIDTH_CODEC
    public static readonly Codec<int> WidthCodec = ExtraCodecs.IntRange(1, 1024);

    //MapCodec dispatches on the type field through the input_control_type registry, maps to vanilla InputControl.MAP_CODEC
    public static readonly MapCodec<InputControl> MapCodec = new InputControlDispatchCodec();
}

//InputControlDispatchCodec dispatches on the type field through the input_control_type registry, maps to vanilla MAP_CODEC
internal sealed class InputControlDispatchCodec : ScalarCodec<InputControl>
{
    public override DataResult<InputControl> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<InputControl>.Error(() => "input control must be an object");
        var map = mapResult.Result().Get();
        var typeValue = map.Get("type");
        if (!typeValue.IsPresent) return DataResult<InputControl>.Error(() => "input control is missing the type field");
        var typeText = ops.GetStringValue(typeValue.Get()).Result();
        if (!typeText.IsPresent) return DataResult<InputControl>.Error(() => "input control type must be a string");
        var id = Identifier.TryParse(typeText.Get());
        if (id is null) return DataResult<InputControl>.Error(() => $"Invalid input control id: {typeText.Get()}");
        var codec = BuiltInRegistries.INPUT_CONTROL_TYPE.GetValue(id.Value);
        if (codec is null) return DataResult<InputControl>.Error(() => $"Unknown input control type: {id.Value}");
        return codec.Decode(ops, map).FlatMap(control => control is InputControl typed
            ? DataResult<InputControl>.Success(typed)
            : DataResult<InputControl>.Error(() => $"Input control {id.Value} is not an input control"));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, InputControl value)
    {
        var codec = value.MapCodec();
        var id = BuiltInRegistries.INPUT_CONTROL_TYPE.GetKey(codec);
        if (id is null) return DataResult<U>.Error(() => "unregistered input control codec");
        return codec.EncodeStart(ops, value).FlatMap(body =>
            ops.MergeToMap(body, ops.CreateString("type"), ops.CreateString(id.ToString())));
    }
}
