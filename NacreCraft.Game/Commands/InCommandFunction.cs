namespace NacreCraft.Game.Commands;

//InCommandFunction a command function that may throw a syntax error, maps to vanilla net.minecraft.server.commands.InCommandFunction
//Used by the function/schedule command plumbing that stores a lambda per command source
public interface InCommandFunction<in T, out R>
{
    R Apply(T value);
}
