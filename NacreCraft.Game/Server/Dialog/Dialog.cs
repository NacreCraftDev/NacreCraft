using NacreCraft.Codec;
using NacreCraft.Registry;
using RegistryDialog = NacreCraft.Registry.Dialog;

namespace NacreCraft.Game.Server.Dialog;

//Dialog one server-defined dialog screen, maps to vanilla net.minecraft.server.dialog.Dialog
//The registry element type is the Registry layer's Dialog marker; this interface adds the data half
public interface Dialog : RegistryDialog
{
    CommonDialogData GetCommon();

    MapCodec<RegistryDialog> Codec();

    Optional<Action> OnCancel();
}

//DialogCodecs shared dialog codecs, maps to the static members of vanilla Dialog
public static class DialogCodecs
{
    //WidthCodec the dialog width range, maps to vanilla Dialog.WIDTH_CODEC
    public static readonly Codec<int> WidthCodec = InputControls.WidthCodec;

    //DirectCodec dispatches on the type field through the dialog_type registry, maps to vanilla DIRECT_CODEC
    public static readonly MapCodec<Dialog> DirectCodec = new DialogDispatchCodec();
}

//DialogDispatchCodec dispatches on the type field through the dialog_type registry, maps to vanilla DIRECT_CODEC
internal sealed class DialogDispatchCodec : ScalarCodec<Dialog>
{
    public override DataResult<Dialog> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<Dialog>.Error(() => "dialog must be an object");
        var map = mapResult.Result().Get();
        var typeValue = map.Get("type");
        if (!typeValue.IsPresent) return DataResult<Dialog>.Error(() => "dialog is missing the type field");
        var typeText = ops.GetStringValue(typeValue.Get()).Result();
        if (!typeText.IsPresent) return DataResult<Dialog>.Error(() => "dialog type must be a string");
        var id = Identifier.TryParse(typeText.Get());
        if (id is null) return DataResult<Dialog>.Error(() => $"Invalid dialog id: {typeText.Get()}");
        var codec = BuiltInRegistries.DIALOG_TYPE.GetValue(id.Value);
        if (codec is null) return DataResult<Dialog>.Error(() => $"Unknown dialog type: {id.Value}");
        return codec.Decode(ops, map).FlatMap(dialog => dialog is Dialog typed
            ? DataResult<Dialog>.Success(typed)
            : DataResult<Dialog>.Error(() => $"Dialog {id.Value} is not a dialog"));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, Dialog value)
    {
        var codec = value.Codec();
        var id = BuiltInRegistries.DIALOG_TYPE.GetKey(codec);
        if (id is null) return DataResult<U>.Error(() => "unregistered dialog codec");
        return codec.EncodeStart(ops, value).FlatMap(body =>
            ops.MergeToMap(body, ops.CreateString("type"), ops.CreateString(id.ToString())));
    }
}

//SimpleDialog a dialog with one row of main actions, maps to vanilla SimpleDialog
public interface SimpleDialog : Dialog
{
    IReadOnlyList<ActionButton> MainActions();
}

//ButtonListDialog a dialog with a grid of buttons, maps to vanilla ButtonListDialog
public interface ButtonListDialog : Dialog
{
    int Columns();

    Optional<ActionButton> ExitAction();

    Optional<Action> Dialog.OnCancel()
    {
        var exitAction = ExitAction();
        return exitAction.IsPresent ? exitAction.Get().ClickAction : Optional<Action>.Empty();
    }
}

