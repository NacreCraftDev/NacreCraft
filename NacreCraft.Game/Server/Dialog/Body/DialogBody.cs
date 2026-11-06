using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Util;
using RegistryDialogBody = NacreCraft.Registry.DialogBody;

namespace NacreCraft.Game.Server.Dialog;

//DialogBody one body element of a dialog, maps to vanilla net.minecraft.server.dialog.body.DialogBody
public interface DialogBody : RegistryDialogBody
{
    MapCodec<RegistryDialogBody> MapCodec();
}

//DialogBodyCodecs shared body codecs, maps to the static members of vanilla DialogBody
public static class DialogBodyCodecs
{
    //DirectCodec dispatches on the type field through the dialog_body_type registry, maps to vanilla DIALOG_BODY_CODEC
    public static readonly Codec<DialogBody> DirectCodec = new DialogBodyDispatchCodec();

    //CompactList accepts one body or a list, maps to vanilla COMPACT_LIST_CODEC
    public static readonly Codec<IReadOnlyList<DialogBody>> CompactListCodec = ExtraCodecs.CompactList(DirectCodec);
}

//DialogBodyDispatchCodec dispatches on the type field through the dialog_body_type registry
internal sealed class DialogBodyDispatchCodec : ScalarCodec<DialogBody>
{
    public override DataResult<DialogBody> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<DialogBody>.Error(() => "dialog body must be an object");
        var map = mapResult.Result().Get();
        var typeValue = map.Get("type");
        if (!typeValue.IsPresent) return DataResult<DialogBody>.Error(() => "dialog body is missing the type field");
        var typeText = ops.GetStringValue(typeValue.Get()).Result();
        if (!typeText.IsPresent) return DataResult<DialogBody>.Error(() => "dialog body type must be a string");
        var id = Identifier.TryParse(typeText.Get());
        if (id is null) return DataResult<DialogBody>.Error(() => $"Invalid dialog body id: {typeText.Get()}");
        var codec = BuiltInRegistries.DIALOG_BODY_TYPE.GetValue(id.Value);
        if (codec is null) return DataResult<DialogBody>.Error(() => $"Unknown dialog body type: {id.Value}");
        return codec.Decode(ops, map).FlatMap(body => body is DialogBody typed
            ? DataResult<DialogBody>.Success(typed)
            : DataResult<DialogBody>.Error(() => $"Dialog body {id.Value} is not a dialog body"));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, DialogBody value)
    {
        var codec = value.MapCodec();
        var id = BuiltInRegistries.DIALOG_BODY_TYPE.GetKey(codec);
        if (id is null) return DataResult<U>.Error(() => "unregistered dialog body codec");
        return codec.EncodeStart(ops, value).FlatMap(body =>
            ops.MergeToMap(body, ops.CreateString("type"), ops.CreateString(id.ToString())));
    }
}
