using NacreCraft.Commands.Tree;

namespace NacreCraft.Commands.Context;

//ParsedCommandNode maps to vanilla com.mojang.brigadier.context.ParsedCommandNode
//Records a CommandNode and its parse range for CommandContext.getNodes to return
public sealed record ParsedCommandNode<S>(CommandNode<S> Node, StringRange Range)
{
    public override string ToString() => $"{Node}@{Range}";
}
