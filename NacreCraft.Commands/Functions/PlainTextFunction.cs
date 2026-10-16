using NacreCraft.Commands.Execution;
using NacreCraft.Nbt;
using NacreCraft.Registry;

namespace NacreCraft.Commands.Functions;

//PlainTextFunction a function without macros, maps to vanilla net.minecraft.commands.functions.PlainTextFunction record
//Entries are already compiled; instantiation returns itself
public sealed record PlainTextFunction<T> : InstantiatedFunction<T>, CommandFunction<T>
   
{
    public Identifier Id { get; }

    public List<UnboundEntryAction<T>> Entries { get; }

    public PlainTextFunction(Identifier id, List<UnboundEntryAction<T>> entries)
    {
        Id = id;
        Entries = entries;
    }

    //Instantiate a function without arguments returns itself directly
    public InstantiatedFunction<T> Instantiate(CompoundTag? arguments, CommandDispatcher<T> dispatcher)
        => this;
}
