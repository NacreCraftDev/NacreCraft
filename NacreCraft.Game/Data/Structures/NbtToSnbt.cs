using System.Security.Cryptography;
using System.Text;
using NacreCraft.Game.Data;
using NacreCraft.Logging;
using NacreCraft.Nbt;

namespace NacreCraft.Game.Data.Structures;

//NbtToSnbt converts binary structure files to their compact SNBT form, maps to vanilla net.minecraft.data.structures.NbtToSnbt
//The output keeps the input folder layout relative to each root; every file is written through the data cache
public sealed class NbtToSnbt : DataProvider
{
    private readonly IReadOnlyList<string> _inputFolders;
    private readonly PackOutput _output;

    public NbtToSnbt(PackOutput output, IReadOnlyCollection<string> inputFolders)
    {
        _output = output;
        _inputFolders = inputFolders.ToList();
    }

    public async Task Run(CachedOutput cache)
    {
        var output = _output.GetOutputFolder();
        var tasks = new List<Task>();
        foreach (var folder in _inputFolders)
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var path in Directory.EnumerateFiles(folder, "*.nbt", SearchOption.AllDirectories))
            {
                var name = GetName(folder, path);
                tasks.Add(Task.Run(() => ConvertStructure(cache, path, name, output)));
            }
        }
        await Task.WhenAll(tasks);
    }

    public string GetName() => "NBT -> SNBT";

    //GetName the output-relative name of a structure file, strips the root prefix and the nbt extension
    private static string GetName(string root, string path)
    {
        var name = Path.GetRelativePath(root, path).Replace('\\', '/');
        return name[..^".nbt".Length];
    }

    //ConvertStructure reads one compressed structure and writes its SNBT form, maps to vanilla convertStructure
    public static async Task<string?> ConvertStructure(CachedOutput cache, string path, string name, string output)
    {
        try
        {
            await using var rawInput = File.OpenRead(path);
            await using var input = new BufferedStream(rawInput);
            var tag = NbtIo.ReadCompressed(input, NbtAccounter.UnlimitedHeap());
            var resultPath = Path.Combine(output, name + ".snbt");
            await WriteSnbt(cache, resultPath, NbtUtils.StructureToSnbt(tag));
            Log.Info($"Converted {name} from NBT to SNBT");
            return resultPath;
        }
        catch (IOException e)
        {
            Log.Error($"Couldn't convert {name} from NBT to SNBT at {path}: {e.Message}");
            return null;
        }
    }

    //WriteSnbt writes the SNBT text with a trailing newline, maps to vanilla writeSnbt
    public static async Task WriteSnbt(CachedOutput cache, string destination, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\n");
        var hash = Convert.ToHexString(SHA1.HashData(bytes));
        await cache.WriteIfNeeded(destination, bytes, hash);
    }
}
