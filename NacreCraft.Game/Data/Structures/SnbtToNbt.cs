using System.Security.Cryptography;
using NacreCraft.Game.Data;
using NacreCraft.Logging;
using NacreCraft.Nbt;

namespace NacreCraft.Game.Data.Structures;

//SnbtToNbt converts compact SNBT structure files back to compressed NBT, maps to vanilla net.minecraft.data.structures.SnbtToNbt
//Filters run between parsing and serialization, which is where StructureUpdater upgrades old templates
public sealed class SnbtToNbt : DataProvider
{
    //Filter transforms one parsed structure by its output name, maps to vanilla SnbtToNbt.Filter
    public interface Filter
    {
        CompoundTag Apply(string name, CompoundTag input);
    }

    private readonly PackOutput _output;
    private readonly IReadOnlyList<string> _inputFolders;
    private readonly List<Filter> _filters = new();

    public SnbtToNbt(PackOutput output, string inputFolder) : this(output, new[] { inputFolder })
    {
    }

    public SnbtToNbt(PackOutput output, IReadOnlyCollection<string> inputFolders)
    {
        _output = output;
        _inputFolders = inputFolders.ToList();
    }

    //AddFilter appends a filter, applied in insertion order
    public SnbtToNbt AddFilter(Filter filter)
    {
        _filters.Add(filter);
        return this;
    }

    public async Task Run(CachedOutput cache)
    {
        var output = _output.GetOutputFolder();
        var tasks = new List<Task>();
        foreach (var folder in _inputFolders)
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var path in Directory.EnumerateFiles(folder, "*.snbt", SearchOption.AllDirectories))
            {
                var name = GetName(folder, path);
                tasks.Add(Task.Run(async () =>
                {
                    var result = ReadStructure(path, name);
                    await StoreStructureIfChanged(cache, result, output);
                }));
            }
        }
        await Task.WhenAll(tasks);
    }

    public string GetName() => "SNBT -> NBT";

    private static string GetName(string root, string path)
    {
        var name = Path.GetRelativePath(root, path).Replace('\\', '/');
        return name[..^".snbt".Length];
    }

    private CompoundTag ApplyFilters(string name, CompoundTag input)
    {
        var result = input;
        foreach (var filter in _filters) result = filter.Apply(name, result);
        return result;
    }

    //ReadStructure parses and filters one file and serializes it to compressed NBT
    //A failure is wrapped with the source path so a broken template is identifiable from the log
    private TaskResult ReadStructure(string path, string name)
    {
        try
        {
            var input = File.ReadAllText(path);
            var updated = ApplyFilters(name, NbtUtils.SnbtToStructure(input));
            using var buffer = new MemoryStream();
            NbtIo.WriteCompressed(updated, buffer);
            var payload = buffer.ToArray();
            var hash = Convert.ToHexString(SHA1.HashData(payload));
            return new TaskResult(name, payload, hash);
        }
        catch (Exception e)
        {
            throw new StructureConversionException(path, e);
        }
    }

    private static async Task StoreStructureIfChanged(CachedOutput cache, TaskResult task, string output)
    {
        var destination = Path.Combine(output, task.Name + ".nbt");
        try
        {
            await cache.WriteIfNeeded(destination, task.Payload, task.Hash);
        }
        catch (IOException e)
        {
            Log.Error($"Couldn't write structure {task.Name} at {destination}: {e.Message}");
        }
    }

    //TaskResult one converted structure: output name, compressed payload and content hash
    private sealed record TaskResult(string Name, byte[] Payload, string Hash);

    //StructureConversionException wraps a parsing or serialization failure with the file path
    private sealed class StructureConversionException(string path, Exception inner)
        : Exception(Path.GetFullPath(path), inner);
}
