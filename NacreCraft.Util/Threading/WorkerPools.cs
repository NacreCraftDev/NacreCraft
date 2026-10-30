namespace NacreCraft.Util.Concurrency;

//Shared pools sized from the core count; CPU work and blocking IO stay apart so one cannot starve the other
public static class WorkerPools
{
    //Cpu leaves one core for the tick and render threads
    public static readonly PrioritisedWorkerPool Cpu =
        new("nacrecraft-cpu", Math.Max(1, Environment.ProcessorCount - 1));

    //Io covers socket writes and region reads; too much concurrency only makes the threads steal cores
    public static readonly PrioritisedWorkerPool Io =
        new("nacrecraft-io", Math.Clamp(Environment.ProcessorCount / 2, 2, 8));
}
