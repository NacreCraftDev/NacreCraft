using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Exceptions;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Commands.Context;
using NacreCraft.Game.Commands;
using NacreCraft.Network.Chat;
using NacreCraft.Network.Protocol.Login;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//GameProfileArgument player profile by name or selector, maps to vanilla net.minecraft.commands.arguments.GameProfileArgument
//The server installs NameLookup and SelectorResolver at startup; without them the argument parses but resolves nothing
public sealed class GameProfileArgument : ArgumentType<GameProfileArgument.Result>
{
    public static readonly SimpleCommandExceptionType ErrorUnknownPlayer =
        new(new TranslatableMessage("argument.player.unknown"));

    private static readonly IReadOnlyList<string> ExampleList = new[]
    {
        "Player", "0123", "dd12be42-52a9-4a91-a8a1-11c01849e498", "@e",
    };

    //NameLookup resolves an online player profile by name
    public static Func<string, GameProfile?>? NameLookup { get; set; }

    //SelectorResolver resolves a selector token to matching playable profiles
    public static Func<string, IReadOnlyList<GameProfile>>? SelectorResolver { get; set; }

    public static GameProfileArgument GameProfile() => new();

    public Result Parse(StringReader reader)
    {
        if (reader.CanRead() && reader.Peek() == '@')
        {
            var selector = ReadToken(reader);
            return _ => SelectorResolver?.Invoke(selector) ?? Array.Empty<GameProfile>();
        }
        var name = ReadToken(reader);
        return _ => NameLookup?.Invoke(name) is { } profile
            ? new[] { profile }
            : throw ErrorUnknownPlayer.Create();
    }

    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
    {
        if (NameLookup is null) return builder.BuildFuture();
        return builder.BuildFuture();
    }

    public IReadOnlyList<string> Examples => ExampleList;

    private static string ReadToken(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        return reader.String[start..reader.Cursor];
    }

    //Result resolves the profiles against a command source, maps to vanilla Result
    public delegate IReadOnlyCollection<GameProfile> Result(CommandSourceStack source);
}
