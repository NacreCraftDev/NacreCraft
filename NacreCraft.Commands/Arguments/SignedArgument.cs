namespace NacreCraft.Commands.Arguments;

//SignedArgument marks an argument type whose parsed text the client signs
//maps to vanilla net.minecraft.commands.arguments.SignedArgument
public interface SignedArgument
{
}

//SignedArgument<T> typed form used by argument implementations, maps to vanilla SignedArgument<T>
public interface SignedArgument<T> : SignedArgument, ArgumentType<T>
{
}
