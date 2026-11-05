using NacreCraft.Codec;
using NacreCraft.Nbt;
using NacreCraft.Network.Chat;
using NacreCraft.Registry;
using RegistryAction = NacreCraft.Registry.Action;

namespace NacreCraft.Game.Server.Dialog;

//Action a dialog button action, maps to vanilla net.minecraft.server.dialog.action.Action
//The registry element type is the Registry layer's Action marker; this interface adds the click-event half
public interface Action : RegistryAction
{
    public static readonly Codec<Action> CODEC = new ActionDispatchCodec();

    MapCodec<RegistryAction> Codec();

    Optional<ClickEvent> CreateAction(IReadOnlyDictionary<string, ValueGetter> parameters);

    //ValueGetter one substitution value, maps to vanilla Action.ValueGetter
    public interface ValueGetter
    {
        string AsTemplateSubstitution();

        Tag AsTag();

        //GetAsTemplateSubstitutions flattens every value to its string form, maps to vanilla getAsTemplateSubstitutions
        public static IReadOnlyDictionary<string, string> GetAsTemplateSubstitutions(
            IReadOnlyDictionary<string, ValueGetter> parameters)
            => parameters.ToDictionary(pair => pair.Key, pair => pair.Value.AsTemplateSubstitution());

        public static ValueGetter Of(string value) => new ConstantValueGetter(value);

        public static ValueGetter Of(Func<string> value) => new SupplierValueGetter(value);
    }
}

internal sealed class ConstantValueGetter(string value) : Action.ValueGetter
{
    public string AsTemplateSubstitution() => value;

    public Tag AsTag() => StringTag.ValueOf(value);
}

internal sealed class SupplierValueGetter(Func<string> value) : Action.ValueGetter
{
    public string AsTemplateSubstitution() => value();

    public Tag AsTag() => StringTag.ValueOf(value());
}

//XmapMapCodec maps a concrete action codec to the registry marker type, the C# stand-in for MapCodec.xmap
internal sealed class XmapMapCodec<TA, TB>(MapCodec<TA> inner, Func<TA, TB> to, Func<TB, TA> from) : MapCodec<TB>
{
    public DataResult<TB> Decode<U>(DynamicOps<U> ops, MapLike<U> input) => inner.Decode(ops, input).Map(to);

    public DataResult<U> EncodeStart<U>(DynamicOps<U> ops, TB value) => inner.EncodeStart(ops, from(value));

    public RecordBuilder<U> EncodeTo<U>(DynamicOps<U> ops, TB value, RecordBuilder<U> builder)
        => inner.EncodeTo(ops, from(value), builder);

    public RecordBuilder<U> Encoder<U>(DynamicOps<U> ops) => inner.Encoder(ops);
}

//ActionDispatchCodec dispatches on the type field through the dialog_action_type registry, maps to vanilla Action.CODEC
internal sealed class ActionDispatchCodec : ScalarCodec<Action>
{
    public override DataResult<Action> Parse<U>(DynamicOps<U> ops, U input)
    {
        var mapResult = ops.GetMap(input);
        if (!mapResult.Result().IsPresent) return DataResult<Action>.Error(() => "action must be an object");
        var map = mapResult.Result().Get();
        var typeValue = map.Get("type");
        if (!typeValue.IsPresent) return DataResult<Action>.Error(() => "action is missing the type field");
        var typeText = ops.GetStringValue(typeValue.Get()).Result();
        if (!typeText.IsPresent) return DataResult<Action>.Error(() => "action type must be a string");
        var id = Identifier.TryParse(typeText.Get());
        if (id is null) return DataResult<Action>.Error(() => $"Invalid action id: {typeText.Get()}");
        var codec = BuiltInRegistries.DIALOG_ACTION_TYPE.GetValue(id.Value);
        if (codec is null) return DataResult<Action>.Error(() => $"Unknown action type: {id.Value}");
        return codec.Decode(ops, map).FlatMap(action => action is Action typed
            ? DataResult<Action>.Success(typed)
            : DataResult<Action>.Error(() => $"Action {id.Value} is not a dialog action"));
    }

    public override DataResult<U> EncodeStart<U>(DynamicOps<U> ops, Action value)
    {
        var codec = value.Codec();
        var id = BuiltInRegistries.DIALOG_ACTION_TYPE.GetKey(codec);
        if (id is null) return DataResult<U>.Error(() => "unregistered action codec");
        return codec.EncodeStart(ops, value).FlatMap(body =>
            ops.MergeToMap(body, ops.CreateString("type"), ops.CreateString(id.ToString())));
    }
}
