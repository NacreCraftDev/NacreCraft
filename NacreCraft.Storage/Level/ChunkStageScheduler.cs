using System.Collections.Concurrent;
using NacreCraft.Logging;
using NacreCraft.Primitives;
using NacreCraft.Registry;
using NacreCraft.Util.Concurrency;

namespace NacreCraft.Storage;

//Runs chunk status stages on the shared CPU pool; a chunk that finishes one stage goes back to the queue
//The pool grows on demand and retires idle threads, so generation shares the core budget with every other CPU task
public sealed class ChunkStageScheduler
{
    //ReportInterval stage completions between queue reports; the pipeline runs about eleven stages per chunk
    private const int ReportInterval = 20;
    private const int ReportTaskCount = 5;
    private const long ReportMinIntervalMs = 1000;

    private readonly IChunkStagePipeline _pipeline;
    private int _queued;
    private int _running;
    private long _stages;
    private long _chunks;
    //_lastReport last printed report, keeps the diagnostic line from flooding the log
    private long _lastReport;
    //Traces carry the chunk position and both the current stage time and the total time since scheduling
    //A single chunk stuck for ten seconds is identified by its position instead of a bare counter
    private readonly ConcurrentDictionary<long, StageTrace> _waiting = new();
    private readonly ConcurrentDictionary<long, StageTrace> _executing = new();

    private readonly record struct StageTrace(ChunkPos Pos, ChunkStatus Status, long Since, long ScheduledAt);

    public ChunkStageScheduler(IChunkStagePipeline pipeline) => _pipeline = pipeline;

    //Enqueue submits a stage; lower priority values run first
    public void Enqueue(ChunkStageTask task)
    {
        var key = task.Chunk.Pos.Pack();
        _waiting[key] = new StageTrace(task.Chunk.Pos, task.Status, Environment.TickCount64, task.Holder.ScheduledAt);
        Interlocked.Increment(ref _queued);
        //Read the ticket level on every enqueue: a chunk created before its ticket level converged would otherwise keep
        //the initial MaxLevel priority for the whole chain and starve behind every later chunk
        var priority = task.Holder.TicketLevel;
        //Ties break on schedule time: the queue is a min-heap, so the smallest key runs first and the chunk that entered
        //the pipeline earliest is served first. Submission order did the opposite, pushing a chunk that already ran ten
        //stages behind every chunk created since, which made a whole view finish in bursts
        var order = (uint)task.Holder.ScheduledAt;
#if NC_REGION_FAIR
        //A region that has already been served many times queues behind one that has not, so two players far apart share
        //the pool instead of the busier view starving the quieter one; inside a region the ordering is still pure age,
        //which keeps the inner ring first. The region is 32x32 chunks, so one player's view falls in a single region
        var served = _regionServed.GetOrAdd(RegionKey(task.Chunk.Pos), 0L) & 0x7FFF;
        var queueKey = ((long)priority << 48) | (served << 32) | order;
#else
        var queueKey = ((long)priority << 32) | order;
#endif
        WorkerPools.Cpu.Enqueue(() => Run(task), queueKey);
    }

    //RegionKey buckets a chunk into its 32x32 chunk region, used only for fair sharing between distant views
    private static long RegionKey(ChunkPos pos)
        => ((long)(pos.X >> 5) << 32) | (uint)(pos.Z >> 5);

    //_regionServed how many chunks each region already had served; drives the fair sharing in Enqueue
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, long> _regionServed = new();
    //_deferred chunks parked because a stage's neighbours had not reached the prerequisite status yet
    private readonly ConcurrentDictionary<long, DeferredStage> _deferred = new();
    private const long DeferredTimeoutMs = 5000;

    private readonly record struct DeferredStage(ChunkStageTask Task, long Since);

    //Run advances one chunk through every remaining stage on this worker, mapping to Paper's ChunkProgressionTask
    //Re-queuing after each stage put a chunk that had already run ten stages behind every chunk created since, and eleven
    //queue waits per chunk is most of what "one chunk took fifteen seconds" was; a chunk now keeps its worker until the
    //chain ends, so the only ordering that can delay it is the order chunks entered the pipeline
    private void Run(ChunkStageTask task)
    {
        var key = task.Chunk.Pos.Pack();
        _waiting.TryRemove(key, out _);
        //Counts as served once it starts, which is what makes the next chunk of this region queue behind other regions
        _regionServed.AddOrUpdate(RegionKey(task.Chunk.Pos), 1L, static (_, value) => value + 1);
        Interlocked.Decrement(ref _queued);
        Interlocked.Increment(ref _running);
        try
        {
            while (true)
            {
                //A stage whose neighbours are not ready is parked instead of run: the worker is released and the chunk is
                //retried when another chunk finishes a stage, so no thread ever waits on a dependency
#if NC_STAGE_DEPS
                if (!_pipeline.CanRunStage(task.Chunk, task.Status))
                {
                    _deferred[key] = new DeferredStage(task, Environment.TickCount64);
                    return;
                }
#endif
                _executing[key] = new StageTrace(task.Chunk.Pos, task.Status, Environment.TickCount64, task.Holder.ScheduledAt);
                task.Holder.MarkStage(task.Status);
                _pipeline.RunStage(task.Chunk, task.Status);
                var next = _pipeline.NextStatus(task.Status);
                if (Interlocked.Increment(ref _stages) % ReportInterval == 0) DiagnosticTick();
                if (next is null)
                {
                    Interlocked.Increment(ref _chunks);
                    task.Completion.TrySetResult(task.Chunk);
#if NC_STAGE_DEPS
                    //A finished chunk is usually what its neighbours were waiting for
                    PumpDeferred();
#endif
                    return;
                }
                task.Status = next;
#if !NC_STAGE_CHAIN
                //Old behaviour: hand the worker back and queue the next stage again
                Enqueue(task);
                return;
#endif
            }
        }
        catch (Exception exception)
        {
            task.Completion.TrySetException(exception);
        }
        finally
        {
            _executing.TryRemove(key, out _);
            Interlocked.Decrement(ref _running);
        }
    }

    //Report prints the queue depth, worker occupancy and the longest-running chunks with their positions
    //A growing queued value with running pinned at the pool size means the stages are the bottleneck, not the scheduling
    private void Report()
    {
        var now = Environment.TickCount64;
        Log.Info($"[chunk-stage] queued={Volatile.Read(ref _queued)} running={Volatile.Read(ref _running)} " +
            $"deferred={_deferred.Count} poolThreads={WorkerPools.Cpu.ThreadCount} " +
            $"stages={Interlocked.Read(ref _stages)} chunks={Interlocked.Read(ref _chunks)} " +
            $"runningTop={Describe(_executing, now)} waitingTop={Describe(_waiting, now)} " +
            $"deferredTop={DescribeDeferred(now)}");
        //The report doubles as the retry tick: parked chunks whose neighbours moved on are queued again here even when
        //no chunk finished in the meantime
#if NC_STAGE_DEPS
        PumpDeferred();
#endif
    }

    //DiagnosticTick reports on a timer so a fully stalled pipeline still prints; stage driven reports stop when nothing runs
    public void DiagnosticTick()
    {
        var now = Environment.TickCount64;
        if (now - _lastReport < ReportMinIntervalMs) return;
        _lastReport = now;
        Report();
    }

    private string DescribeDeferred(long now)
    {
#if NC_STAGE_DEPS
        if (_deferred.IsEmpty) return "none";
        return string.Join(",", _deferred.Values
            .OrderByDescending(pending => now - pending.Since)
            .Take(ReportTaskCount)
            .Select(pending => $"[{pending.Task.Chunk.Pos.X},{pending.Task.Chunk.Pos.Z}]" +
                $"{pending.Task.Status}:{now - pending.Since}ms"));
#else
        return "off";
#endif
    }

    //PumpDeferred retries the parked chunks whose neighbours have reached the prerequisite status
    //The timeout keeps a chunk from waiting forever on a neighbour that is never going to arrive
    private void PumpDeferred()
    {
        if (_deferred.IsEmpty) return;
        var now = Environment.TickCount64;
        foreach (var pair in _deferred)
        {
            var pending = pair.Value;
            if (now - pending.Since < DeferredTimeoutMs
                && !_pipeline.CanRunStage(pending.Task.Chunk, pending.Task.Status)) continue;
            if (!_deferred.TryRemove(pair.Key, out var removed)) continue;
            Enqueue(removed.Task);
        }
    }

    private static string Describe(ConcurrentDictionary<long, StageTrace> traces, long now)
    {
        if (traces.IsEmpty) return "none";
        return string.Join(",", traces.Values
            .OrderByDescending(trace => now - trace.ScheduledAt)
            .Take(ReportTaskCount)
            .Select(trace => $"[{trace.Pos.X},{trace.Pos.Z}]{trace.Status}:{now - trace.Since}ms/{now - trace.ScheduledAt}ms"));
    }
}
