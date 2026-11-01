using NacreCraft.Commands;
using NacreCraft.Commands.Tree;
using NacreCraft.Game.Commands;
using NacreCraft.Game.Server;

namespace NacreCraft.Server.ServerConsole;

//Completion one completion candidate
//Text is the whole input after the candidate is applied, Level is the permission the command it names requires, so the
//console can colour it by how much damage it can do
public readonly record struct Completion(string Text, int Level);

//ICompletionSource, completion source
//Implementations return the "full input text after applying the candidate", the console replaces the whole line with it and need not track ranges
public interface ICompletionSource
{
    //GetCompletions computes candidates, text is the current input and caret is the cursor position
    IReadOnlyList<Completion> GetCompletions(string text, int caret);
}

//CommandCompletionSource computes completions from the brigadier command tree
//Uses the same dispatcher as pressing Tab in-game, both subcommands and arguments can be completed
public sealed class CommandCompletionSource : ICompletionSource
{
    private readonly DedicatedServer _server;

    public CommandCompletionSource(DedicatedServer server) => _server = server;

    public IReadOnlyList<Completion> GetCompletions(string text, int caret)
    {
        var source = ServerCommandSource.Console(_server);
        var suggestions = _server.Commands.GetCompletions(source, text, caret);
        if (suggestions.IsEmpty()) return Array.Empty<Completion>();

        var root = _server.Commands.Dispatcher.GetRoot();
        var results = new List<Completion>(suggestions.List.Count);
        foreach (var suggestion in suggestions.List)
        {
            var applied = suggestion.Apply(text);
            results.Add(new Completion(applied, RequiredLevel(root, applied)));
        }

        return results;
    }

    //RequiredLevel reports the lowest permission level at which the command runs
    //
    //A node's requirement is a predicate over the source rather than a number, so the level is found by asking it with a
    //source built for each level in turn. A requirement that also asks about the source kind answers nothing that way,
    //and such a command is shown at the top level instead of pretending to be open to everyone
    private static int RequiredLevel(CommandNode<CommandSourceStack> root, string applied)
    {
        //Only the first word names the command, everything after it is arguments
        var end = applied.IndexOf(' ');
        var name = end < 0 ? applied : applied[..end];
        if (name.Length == 0) return 0;

        var node = root.GetChild(name);
        if (node is null) return 0;

        for (var level = 0; level <= 4; level++)
        {
            if (node.CanUse(new CommandSourceStack("probe", level, TextWriter.Null))) return level;
        }

        return 4;
    }
}
