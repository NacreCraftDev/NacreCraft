using NacreCraft.Commands.Arguments;

namespace NacreCraft.Commands;

//ArgumentVisitor walks the argument nodes of a parse result and their parsed values
//maps to vanilla net.minecraft.commands.ArgumentVisitor
//Java's Output is generic per argument type; the untyped ArgumentCommandNode base lets this port pass the node without the wildcard
public static class ArgumentVisitor
{
    public static void VisitArguments<S>(ParseResults<S> command, Output<S> output, bool rejectRootRedirects)
    {
        var rootContext = command.GetContext();
        var context = rootContext;
        VisitNodeArguments(context, output);
        while (true)
        {
            var child = context.GetChild();
            if (child is null || (rejectRootRedirects && ReferenceEquals(child.GetRootNode(), rootContext.GetRootNode())))
                break;
            VisitNodeArguments(child, output);
            context = child;
        }
    }

    private static void VisitNodeArguments<S>(Context.CommandContextBuilder<S> context, Output<S> output)
    {
        var values = context.GetArguments();
        foreach (var parsedNode in context.GetNodes())
        {
            if (parsedNode.Node is not Tree.ArgumentCommandNode<S> argument) continue;
            values.TryGetValue(argument.Name, out var value);
            output.Accept(context, argument, value);
        }
    }

    //Output receives every argument node with its parsed value, null when the node did not consume input
    public interface Output<S>
    {
        void Accept(Context.CommandContextBuilder<S> context, Tree.ArgumentCommandNode<S> argument,
            Context.ParsedArgument<S>? value);
    }
}
