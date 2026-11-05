using NacreCraft.Logging;

namespace NacreCraft.Storage;

//FileRelation maps the world root to the files a fix touches, maps to vanilla net.minecraft.util.filefix.access.FileRelation
//Vanilla is a functional interface with constants and combinators; here the delegate carries the relation and the static
//helpers carry the combinators, paths are strings to match the storage layer
public delegate List<string> FileRelation(string basePath);

public static class FileRelations
{
    //ORIGIN the world root itself, maps to vanilla ORIGIN (List::of used as a single-path list)
    public static readonly FileRelation ORIGIN = basePath => new List<string> { basePath };

    //REGION the overworld region folder, maps to vanilla REGION
    public static readonly FileRelation REGION = basePath => new List<string> { Path.Combine(basePath, "region") };

    //DATA the data folder, maps to vanilla DATA
    public static readonly FileRelation DATA = basePath => new List<string> { Path.Combine(basePath, "data") };

    //PLAYER_DATA the player data folder, maps to vanilla PLAYER_DATA
    public static readonly FileRelation PLAYER_DATA = basePath => new List<string> { Path.Combine(basePath, "players", "data") };

    //DIMENSIONS every dimension folder, maps to vanilla DIMENSIONS
    public static readonly FileRelation DIMENSIONS = DiscoverDimensions;

    //DIMENSIONS_DATA the data folder of every dimension, maps to vanilla DIMENSIONS_DATA
    public static readonly FileRelation DIMENSIONS_DATA = DIMENSIONS.Resolve(DATA);

    //GENERATED_NAMESPACES the generated namespace folders, maps to vanilla GENERATED_NAMESPACES
    public static readonly FileRelation GENERATED_NAMESPACES =
        ORIGIN.Resolve(basePath => DirectoriesInPath(Path.Combine(basePath, "generated")));

    //OLD_OVERWORLD the pre-dimension overworld files sitting at the world root, maps to vanilla OLD_OVERWORLD
    public static readonly FileRelation OLD_OVERWORLD = ORIGIN;

    //OLD_NETHER the legacy DIM-1 folder, maps to vanilla OLD_NETHER
    public static readonly FileRelation OLD_NETHER = ORIGIN.Resolve(basePath => new List<string> { Path.Combine(basePath, "DIM-1") });

    //OLD_END the legacy DIM1 folder, maps to vanilla OLD_END
    public static readonly FileRelation OLD_END = ORIGIN.Resolve(basePath => new List<string> { Path.Combine(basePath, "DIM1") });

    //ForDataFileInDimension the data file of one dimension, maps to vanilla forDataFileInDimension
    public static FileRelation ForDataFileInDimension(string dimension, string fileName)
        => basePath => new List<string>
        {
            Path.Combine(basePath, "dimensions", "minecraft", dimension, "data", fileName)
        };

    //ForFile appends one file name to every resolved folder, maps to vanilla forFile
    public static FileRelation ForFile(this FileRelation relation, string fileName)
        => relation.Resolve(basePath => new List<string> { Path.Combine(basePath, fileName) });

    //Resolve chains two relations, maps to vanilla resolve
    public static FileRelation Resolve(this FileRelation relation, FileRelation other)
        => basePath => relation(basePath).SelectMany(path => other(path)).ToList();

    //Join unions several relations, maps to vanilla join
    public static FileRelation Join(this FileRelation relation, params FileRelation[] others)
        => basePath =>
        {
            var paths = new HashSet<string>();
            paths.UnionWith(relation(basePath));
            foreach (var other in others) paths.UnionWith(other(basePath));
            return paths.ToList();
        };

    //DiscoverDimensions lists every dimension folder under dimensions, falling back to the three vanilla dimensions
    //maps to vanilla discoverDimensions
    private static List<string> DiscoverDimensions(string basePath)
    {
        var dimensionsRoot = Path.Combine(basePath, "dimensions");
        if (!Directory.Exists(dimensionsRoot)) return GetDefaultDimensions(basePath);
        try
        {
            var discovered = Directory.GetDirectories(dimensionsRoot)
                .SelectMany(DirectoriesInPath)
                .ToList();
            return discovered.Count == 0 ? GetDefaultDimensions(basePath) : discovered;
        }
        catch (IOException e)
        {
            Log.Warning($"Failed to discover dimensions, assuming default: {e}");
            return GetDefaultDimensions(basePath);
        }
    }

    //DirectoriesInPath lists the subdirectories of a folder, maps to vanilla directoriesInPath
    public static List<string> DirectoriesInPath(string path)
    {
        try
        {
            return Directory.GetDirectories(path).ToList();
        }
        catch (IOException)
        {
            return new List<string>();
        }
    }

    private static List<string> GetDefaultDimensions(string basePath)
        => new()
        {
            Path.Combine(basePath, "dimensions", "minecraft", "overworld"),
            Path.Combine(basePath, "dimensions", "minecraft", "the_nether"),
            Path.Combine(basePath, "dimensions", "minecraft", "the_end")
        };
}
