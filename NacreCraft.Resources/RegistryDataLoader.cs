using System.Diagnostics;
using System.Text.Json;
using NacreCraft.Codec;
using NacreCraft.Logging;
using NacreCraft.Registry;

namespace NacreCraft.Resources;

//RegistryDataLoader, data-driven registry loader, maps to vanilla RegistryDataLoader
//Scans resource packs at data/<namespace>/<registry path>/<element path>.json, decodes with the element codec and writes into the target registry
//A single bad element is only recorded in the result and does not abort the whole load, vanilla aggregates per-element exceptions into a CrashReport
public static class RegistryDataLoader
{
    //Load loads a set of registry data, must be called before the target registries are frozen
    //context provides the lookup entry point for resolving cross-registry references inside elements
    public static LoadResult Load(ResourceManager resourceManager, RegistryAccess context,
        IReadOnlyList<RegistryData> data)
    {
        //First scan the elements of all registries into one combined queue, elements reference each other, including across registries (configured_feature references placed_feature)
        //Vanilla loads registries in parallel so interleaved references resolve, here the whole queue is retried repeatedly to reproduce the same effect
        var scanWatch = Stopwatch.StartNew();
        var pending = new List<PendingElement>();
        foreach (var entry in data) CollectPending(resourceManager, entry, pending);
        scanWatch.Stop();

        //Read and parse every file up front in parallel; files are independent and this is the most expensive stage
        //A parse failure is final, retrying the same bytes can only fail the same way, so such elements never enter the retry queue
        var parseWatch = Stopwatch.StartNew();
        var documents = new JsonDocument?[pending.Count];
        var parseErrors = new string?[pending.Count];
        Parallel.For(0, pending.Count, i =>
        {
            try
            {
                using var stream = pending[i].Resource.Open();
                documents[i] = JsonElementOps.Parse(stream).GetOrThrow();
            }
            catch (Exception ex)
            {
                parseErrors[i] = $"{pending[i].Id}: {ex.Message}";
            }
        });
        parseWatch.Stop();

        var decodeWatch = Stopwatch.StartNew();
        try
        {
            var queue = new List<PendingElement>(pending.Count);
            var errors = new List<string>();
            for (var i = 0; i < pending.Count; i++)
            {
                if (parseErrors[i] is { } error) errors.Add(error);
                else queue.Add(pending[i] with { Document = documents[i] });
            }

            //Pre-create the standalone holder of every element, so any element referencing a later one resolves to its holder
            //This mirrors vanilla createRegistrationLookup: decoding never needs the target value to exist yet, Register binds it later
            foreach (var item in queue) item.Data.PrepareHolder(item.Id);

            //Decode in parallel: every registry reference is now a pure key-index read, so decoding writes no registry state
            //Only the tag sets are created on first sight and they live in a ConcurrentDictionary, so there is no lock on this path
            //Dedicated threads instead of Parallel.For: the pool ramps up lazily at process start and would leave most cores idle
            var values = new object?[queue.Count];
            var decodeErrors = new string?[queue.Count];
            void DecodeOne(int index)
            {
                var item = queue[index];
                if (item.Data.TryDecode(item.Document!.RootElement, item.Id, context, out var value, out var error))
                    values[index] = value;
                else
                    decodeErrors[index] = error;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, queue.Count);
            if (workerCount <= 1)
            {
                for (var i = 0; i < queue.Count; i++) DecodeOne(i);
            }
            else
            {
                var workers = new Thread[workerCount];
                for (var w = 0; w < workerCount; w++)
                {
                    var start = (int)((long)w * queue.Count / workerCount);
                    var end = (int)((long)(w + 1) * queue.Count / workerCount);
                    workers[w] = new Thread(() =>
                    {
                        for (var i = start; i < end; i++) DecodeOne(i);
                    })
                    { IsBackground = true, Name = $"RegistryDecode-{w}" };
                    workers[w].Start();
                }
                foreach (var worker in workers) worker.Join();
            }

            //Registration stays on this thread, it is the only pass that writes the registries
            var loaded = 0;
            var failed = new List<PendingElement>();
            for (var i = 0; i < queue.Count; i++)
            {
                var item = queue[i];
                if (decodeErrors[i] is { } error)
                {
                    errors.Add(error);
                    failed.Add(item);
                    continue;
                }
                try
                {
                    item.Data.Register(item.Id, values[i]!);
                    loaded++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.Id}: {ex.Message}");
                    failed.Add(item);
                }
            }
            //A later element referencing a failed one re-creates its placeholder while decoding
            //Drop those again so freeze does not report an element whose failure is already recorded
            foreach (var item in failed) item.Data.RemoveHolder(item.Id);
            return new LoadResult(loaded, errors);
        }
        finally
        {
            decodeWatch.Stop();
            foreach (var document in documents) document?.Dispose();
            Log.Info($"Registry data timing scan={scanWatch.ElapsedMilliseconds}ms parse={parseWatch.ElapsedMilliseconds}ms decode={decodeWatch.ElapsedMilliseconds}ms files={pending.Count}");
        }
    }

    //CollectPending scans all element files under a registry directory and queues them in directory order
    private static void CollectPending(ResourceManager resourceManager, RegistryData data,
        List<PendingElement> pending)
    {
        var folder = data.RegistryId.Path;
        var prefix = folder + "/";

        //Walk all namespaces, using the same pack traversal as tag loading
        foreach (var ns in resourceManager.GetNamespaces(PackType.ServerData))
        {
            foreach (var resource in resourceManager.ListResources(PackType.ServerData, ns, folder))
            {
                var path = resource.Location.Path;
                if (!path.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var elementPath = path[prefix.Length..];
                if (elementPath.EndsWith(".json", StringComparison.Ordinal))
                    elementPath = elementPath[..^5];
                if (elementPath.Length == 0) continue;

                var elementId = Identifier.FromNamespaceAndPath(resource.Location.Namespace, elementPath);
                pending.Add(new PendingElement(data, resource, elementId, null, string.Empty));
            }
        }
    }
}

//PendingElement, an element waiting to load, carries the parsed document so retries never read the file again
internal readonly record struct PendingElement(RegistryData Data, Resource Resource, Identifier Id, JsonDocument? Document, string Error);

//LoadResult, carries the success count and per-element errors
public sealed class LoadResult
{
    public LoadResult(int loadedCount, IReadOnlyList<string> errors)
    {
        LoadedCount = loadedCount;
        Errors = errors;
    }

    //LoadedCount is the number of elements successfully written to a registry
    public int LoadedCount { get; }

    //Errors are per-element failure reasons, shaped like minecraft:foo: message
    public IReadOnlyList<string> Errors { get; }

    public bool HasErrors => Errors.Count > 0;
}
