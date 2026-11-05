using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using RegistryAction = NacreCraft.Registry.Action;

namespace NacreCraft.Game.Server.Dialog;

//StaticAction a fixed click event, maps to vanilla net.minecraft.server.dialog.action.StaticAction
public sealed record StaticAction(ClickEvent Value) : Action
{
    //WRAPPED_CODECS one codec per server-allowed click action, maps to vanilla WRAPPED_CODECS
    public static readonly IReadOnlyDictionary<ClickEvent.Action, MapCodec<RegistryAction>> WRAPPED_CODECS = BuildCodecs();

    public MapCodec<RegistryAction> Codec() => WRAPPED_CODECS[Value.EventAction];

    public Optional<ClickEvent> CreateAction(IReadOnlyDictionary<string, Action.ValueGetter> parameters)
        => Optional<ClickEvent>.Of(Value);

    private static IReadOnlyDictionary<ClickEvent.Action, MapCodec<RegistryAction>> BuildCodecs()
    {
        var codecs = new Dictionary<ClickEvent.Action, MapCodec<RegistryAction>>
        {
            [ClickEvent.Action.OpenUrl] = Wrap(ClickEvent.OpenUrl.CODEC),
            [ClickEvent.Action.RunCommand] = Wrap(ClickEvent.RunCommand.CODEC),
            [ClickEvent.Action.SuggestCommand] = Wrap(ClickEvent.SuggestCommand.CODEC),
            [ClickEvent.Action.ChangePage] = Wrap(ClickEvent.ChangePage.CODEC),
            [ClickEvent.Action.CopyToClipboard] = Wrap(ClickEvent.CopyToClipboard.CODEC),
            [ClickEvent.Action.Custom] = Wrap(ClickEvent.Custom.CODEC)
        };
        return codecs;
    }

    private static MapCodec<RegistryAction> Wrap<T>(MapCodec<T> codec) where T : ClickEvent
        => new XmapMapCodec<T, RegistryAction>(codec, value => new StaticAction(value),
            action => (T)((StaticAction)action).Value);
}

