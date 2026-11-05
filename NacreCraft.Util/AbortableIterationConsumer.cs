namespace NacreCraft.Util;

//IterationContinuation whether the caller should keep iterating, maps to vanilla AbortableIterationConsumer.Continuation
//Hoisted out of the generic interface because C# cannot put a shared enum inside an open generic
public enum IterationContinuation
{
    CONTINUE,
    ABORT
}

public static class IterationContinuations
{
    //ShouldAbort whether the continuation asks the loop to stop, maps to vanilla shouldAbort
    public static bool ShouldAbort(this IterationContinuation continuation) => continuation == IterationContinuation.ABORT;
}

//AbortableIterationConsumer receives elements until it asks to stop, maps to vanilla net.minecraft.util.AbortableIterationConsumer
public interface AbortableIterationConsumer<T>
{
    IterationContinuation Accept(T value);
}

public static class AbortableIterationConsumers
{
    //ForConsumer wraps a plain consumer that never aborts, maps to vanilla forConsumer
    public static AbortableIterationConsumer<T> ForConsumer<T>(Action<T> consumer) => new ConsumerAdapter<T>(consumer);

    private sealed class ConsumerAdapter<T>(Action<T> consumer) : AbortableIterationConsumer<T>
    {
        public IterationContinuation Accept(T value)
        {
            consumer(value);
            return IterationContinuation.CONTINUE;
        }
    }
}
