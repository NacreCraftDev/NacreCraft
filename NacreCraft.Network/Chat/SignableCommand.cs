using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Tree;

namespace NacreCraft.Network.Chat;

//SignableCommand the signable arguments of a parsed command, maps to vanilla net.minecraft.network.chat.SignableCommand
public sealed record SignableCommand<S>(IReadOnlyList<SignableCommand<S>.Argument> Arguments)
{
    public static bool HasSignableArguments(ParseResults<S> command) => Of(command).Arguments.Count > 0;

    //Of collects the arguments whose type is signable, maps to vanilla of
    public static SignableCommand<S> Of(ParseResults<S> command)
    {
        var commandString = command.GetReader().String;
        var arguments = new List<Argument>();
        ArgumentVisitor.VisitArguments(command, new Collector(commandString, arguments), true);
        return new SignableCommand<S>(arguments);
    }

    public Argument? GetArgument(string name) => Arguments.FirstOrDefault(argument => argument.Name == name);

    //Collector appends every signable argument as it is visited
    private sealed class Collector(string commandString, List<Argument> arguments) : ArgumentVisitor.Output<S>
    {
        public void Accept(CommandContextBuilder<S> context, ArgumentCommandNode<S> argument, ParsedArgument<S>? value)
        {
            if (value is null || argument.GetArgumentTypeObject() is not SignedArgument) return;
            arguments.Add(new Argument(argument, value.Range.Get(commandString)));
        }
    }

    //Argument one signable argument node and its raw text
    public sealed record Argument(ArgumentCommandNode<S> Node, string Value)
    {
        public string Name => Node.Name;
    }
}
