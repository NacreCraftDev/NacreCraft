using NacreCraft.Codec;
using NacreCraft.Nbt;
using NacreCraft.Registry;
using NacreCraft.Registry.Codec;

namespace NacreCraft.Network.Chat;

//Click event, maps to vanilla net.minecraft.network.chat.ClickEvent
//The action triggered when the text is clicked, such as opening a URL/running a command/changing page
public interface ClickEvent
{
    //The action enum, maps to vanilla ClickEvent.Action
    //Identifies the click event type and carries its corresponding MapCodec
    public enum Action
    {
        OpenUrl,
        OpenFile,
        RunCommand,
        SuggestCommand,
        ChangePage,
        CopyToClipboard,
        Custom
    }

    public Action EventAction { get; }

    //Open URL, maps to vanilla ClickEvent.OpenUrl
    public sealed record OpenUrl(Uri Uri) : ClickEvent
    {
        public static readonly MapCodec<OpenUrl> CODEC = RecordCodecBuilder.Of1(
            Codecs.String.ComapFlatMap(ParseUri, uri => uri.ToString()).FieldOf("url")
                .ForGetter((OpenUrl value) => value.Uri),
            uri => new OpenUrl(uri));

        public Action EventAction => Action.OpenUrl;

        private static DataResult<Uri> ParseUri(string value)
            => Uri.TryCreate(value, UriKind.Absolute, out var uri)
                ? DataResult<Uri>.Success(uri)
                : DataResult<Uri>.Error(() => $"Invalid URL: {value}");
    }

    //Open file, maps to vanilla ClickEvent.OpenFile
    public sealed record OpenFile(string Path) : ClickEvent
    {
        public static readonly MapCodec<OpenFile> CODEC = RecordCodecBuilder.Of1(
            Codecs.String.FieldOf("path").ForGetter((OpenFile value) => value.Path),
            path => new OpenFile(path));

        public Action EventAction => Action.OpenFile;
    }

    //Run command, maps to vanilla ClickEvent.RunCommand
    public sealed record RunCommand(string Command) : ClickEvent
    {
        public static readonly MapCodec<RunCommand> CODEC = RecordCodecBuilder.Of1(
            Codecs.String.FieldOf("command").ForGetter((RunCommand value) => value.Command),
            command => new RunCommand(command));

        public Action EventAction => Action.RunCommand;
    }

    //Suggest command, maps to vanilla ClickEvent.SuggestCommand
    public sealed record SuggestCommand(string Command) : ClickEvent
    {
        public static readonly MapCodec<SuggestCommand> CODEC = RecordCodecBuilder.Of1(
            Codecs.String.FieldOf("command").ForGetter((SuggestCommand value) => value.Command),
            command => new SuggestCommand(command));

        public Action EventAction => Action.SuggestCommand;
    }

    //Change page, maps to vanilla ClickEvent.ChangePage
    public sealed record ChangePage(int Page) : ClickEvent
    {
        public static readonly MapCodec<ChangePage> CODEC = RecordCodecBuilder.Of1(
            Codecs.Int.FieldOf("page").ForGetter((ChangePage value) => value.Page),
            page => new ChangePage(page));

        public Action EventAction => Action.ChangePage;
    }

    //Copy to clipboard, maps to vanilla ClickEvent.CopyToClipboard
    public sealed record CopyToClipboard(string Value) : ClickEvent
    {
        public static readonly MapCodec<CopyToClipboard> CODEC = RecordCodecBuilder.Of1(
            Codecs.String.FieldOf("value").ForGetter((CopyToClipboard value) => value.Value),
            value => new CopyToClipboard(value));

        public Action EventAction => Action.CopyToClipboard;
    }

    //Custom server payload, maps to vanilla ClickEvent.Custom
    //Vanilla carries any NBT tag as payload; only compound payloads are ported here
    public sealed record Custom(Identifier Id, Optional<CompoundTag> Payload) : ClickEvent
    {
        public static readonly MapCodec<Custom> CODEC = RecordCodecBuilder.Of2(
            IdentifierCodec.Instance.FieldOf("id").ForGetter((Custom value) => value.Id),
            CompoundTag.Codec.OptionalFieldOf("payload").ForGetter((Custom value) => value.Payload),
            (id, payload) => new Custom(id, payload));

        public Action EventAction => Action.Custom;
    }
}

//ClickEventActions serialized names and server eligibility of the click actions, maps to vanilla ClickEvent.Action methods
public static class ClickEventActions
{
    //GetSerializedName the save name of the action, maps to vanilla getSerializedName
    public static string GetSerializedName(this ClickEvent.Action action) => action switch
    {
        ClickEvent.Action.OpenUrl => "open_url",
        ClickEvent.Action.OpenFile => "open_file",
        ClickEvent.Action.RunCommand => "run_command",
        ClickEvent.Action.SuggestCommand => "suggest_command",
        ClickEvent.Action.ChangePage => "change_page",
        ClickEvent.Action.CopyToClipboard => "copy_to_clipboard",
        _ => "custom"
    };

    //IsAllowedFromServer whether a server-side dialog may use the action, maps to vanilla isAllowedFromServer
    //OpenFile touches the client filesystem and is therefore client-only
    public static bool IsAllowedFromServer(this ClickEvent.Action action) => action != ClickEvent.Action.OpenFile;
}
