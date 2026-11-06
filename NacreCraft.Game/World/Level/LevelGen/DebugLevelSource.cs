using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Chunk;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//DebugLevelSource debug world generator, maps to vanilla net.minecraft.world.level.levelgen.DebugLevelSource
//Lays out every block state in a grid on a 70-high platform; the debug world exists to inspect block models and states
public sealed class DebugLevelSource : ChunkGenerator
{
    public const int Height = 70;
    public const int BarrierHeight = 60;

    public override Identifier? GetTypeNameForDataFixer() => Identifier.WithDefaultNamespace("debug");

    private static readonly BlockState AirState = Blocks.AIR.DefaultBlockState;

    //AllBlocks every registered block state in registry order, maps to vanilla ALL_BLOCKS
    private static List<BlockState>? _allBlocks;

    private static List<BlockState> AllBlocks
        => _allBlocks ??= BuiltInRegistries.BLOCK.EntrySet.SelectMany(entry => entry.Value.AllStates).ToList();

    private static readonly int GridWidth = (int)Math.Ceiling(Math.Sqrt(AllBlocks.Count));
    private static readonly int GridHeight = (int)Math.Ceiling(AllBlocks.Count / (double)GridWidth);

    //BarrierState barrier block state, the platform floor of the debug world
    private static BlockState BarrierState
        => BuiltInRegistries.BLOCK.GetValue(Identifier.WithDefaultNamespace("barrier"))?.DefaultBlockState ?? AirState;

    public DebugLevelSource(Biome plains) : base(new FixedBiomeSource(plains))
    {
    }

    //GetBlockStateFor picks the grid cell's block state, maps to vanilla getBlockStateFor
    public static BlockState GetBlockStateFor(int worldX, int worldZ)
    {
        var state = AirState;
        if (worldX <= 0 || worldZ <= 0 || worldX % 2 == 0 || worldZ % 2 == 0) return state;
        var gridX = worldX / 2;
        var gridZ = worldZ / 2;
        if (gridX > GridWidth || gridZ > GridHeight) return state;
        var index = Math.Abs(gridX * GridWidth + gridZ);
        return index < AllBlocks.Count ? AllBlocks[index] : state;
    }

    //ApplyBiomeDecoration places the barrier floor and the grid row, maps to vanilla applyBiomeDecoration
    public override void ApplyBiomeDecoration(WorldGenRegion region, ChunkAccess chunk, StructureFeatureManager structures)
    {
        var baseX = chunk.Pos.X * 16;
        var baseZ = chunk.Pos.Z * 16;
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            var worldX = baseX + x;
            var worldZ = baseZ + z;
            region.SetBlockState(worldX, BarrierHeight, worldZ, BarrierState);
            region.SetBlockState(worldX, Height, worldZ, GetBlockStateFor(worldX, worldZ));
        }
    }

    public override void FillFromNoise(object blender, object structures, ChunkAccess chunk, RandomSource random)
    {
    }

    public override void BuildSurface(object region, object structures, ChunkAccess chunk, RandomSource random)
    {
    }

    public override void ApplyCarvers(long seed, ChunkAccess chunk, RandomSource random)
    {
    }

    public override int GetBaseHeight(int x, int z, int type, LevelHeightAccessor level, RandomSource random) => 0;

    public override object[] GetBaseColumn(int x, int z, LevelHeightAccessor level, RandomSource random)
        => Array.Empty<object>();

    public override int GetMinY() => 0;

    public override int GetGenDepth() => 384;

    //GetSeaLevel sea level of the debug world, maps to vanilla getSeaLevel
    public int GetSeaLevel() => 63;
}
