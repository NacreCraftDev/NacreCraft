using System.Runtime.CompilerServices;

namespace NcnHookTest.Guard;

//StackTrip is the entry the machine-code guard jumps to when the stack runs short
//
//It sits in an assembly of its own because the guard never hooks this assembly: a trip that landed on a hooked entry
//would be checked again by the trampoline and thrown back into itself. The product puts the same entry in
//NacreCraft.Util, which is excluded by name.
public static class StackTrip
{
    //Trip is entered with the caller's return address on the stack and no frame of its own
    //The throw therefore unwinds exactly as if the method whose entry was replaced had thrown it
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Trip() => throw new InsufficientExecutionStackException();
}
