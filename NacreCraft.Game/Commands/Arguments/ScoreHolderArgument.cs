using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Suggestion;
using NacreCraft.Game.Commands;
using StringReader = NacreCraft.Commands.StringReader;

namespace NacreCraft.Game.Commands.Arguments;

//ScoreHolderArgument score holder names, maps to vanilla net.minecraft.commands.arguments.ScoreHolderArgument
//The scoreboard is not ported, so the token is returned as-is and resolved by the caller
public sealed class ScoreHolderArgument : ArgumentType<ScoreHolderArgument.Result>
{
    private readonly bool _multiple;

    public ScoreHolderArgument(bool multiple) => _multiple = multiple;

    public static ScoreHolderArgument ScoreHolder() => new(false);

    public static ScoreHolderArgument ScoreHolders() => new(true);

    //Resolver installed by the server to expand selectors into names
    public static Func<string, IReadOnlyList<string>>? Resolver { get; set; }

    public Result Parse(StringReader reader)
    {
        var start = reader.Cursor;
        while (reader.CanRead() && reader.Peek() != ' ') reader.Skip();
        var token = reader.String[start..reader.Cursor];
        return _ => Resolver?.Invoke(token) ?? (IReadOnlyList<string>)new[] { token };
    }

    public Task<Suggestions> ListSuggestions<S>(CommandContext<S> context, SuggestionsBuilder builder)
        => builder.BuildFuture();

    //Result resolves score holder names, maps to vanilla Result
    public delegate IReadOnlyCollection<string> Result(CommandSourceStack source);
}
