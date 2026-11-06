using NacreCraft.Game.World.Level.Block;
using NacreCraft.Game.World.Level.LevelGen.Structure;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Storage;
using NacreCraft.Storage.Chunk;
using NacreCraft.Util.Random;

namespace NacreCraft.Game.World.Level.LevelGen;

//FlatLevelSource superflat world generator, maps to vanilla net.minecraft.world.level.levelgen.FlatLevelSource
//Vanilla takes a FlatLevelGeneratorSettings; the settings type is not ported yet, so the layer list is passed directly
//and the structure override / biome lookup parts of the settings are degraded away
public sealed class FlatLevelSource : ChunkGenerator
{
    private readonly IReadOnlyList<BlockState?> _layers;

    public FlatLevelSource(Biome biome, IReadOnlyList<BlockState?> layers)
        : base(new FixedBiomeSource(biome))
    {
        _layers = layers;
    }

    public IReadOnlyList<BlockState?> Layers => _layers;

    public override Identifier? GetTypeNameForDataFixer() => Identifier.WithDefaultNamespace("flat");

    //FillFromNoise writes the layer stack from the bottom of the chunk, maps to vanilla fillFromNoise
    public override void FillFromNoise(object blender, object structures, ChunkAccess chunk, RandomSource random)
    {
        var oceanFloor = chunk.GetOrCreateHeightmapForType(NacreCraft.Registry.Heightmap.Types.OceanFloorWg);
        var worldSurface = chunk.GetOrCreateHeightmapForType(NacreCraft.Registry.Heightmap.Types.WorldSurfaceWg);
        //Default interface members are only reachable through the interface, so derive the build bounds from the section range
        var minBuildHeight = chunk.MinSectionY * 16;
        var maxBuildHeight = (chunk.MaxSectionY + 1) * 16;
        var limit = Math.Min(maxBuildHeight - minBuildHeight, _layers.Count);
        for (var layerIndex = 0; layerIndex < limit; layerIndex++)
        {
            var state = _layers[layerIndex];
            if (state is null) continue;
            var y = minBuildHeight + layerIndex;
            for (var x = 0; x < 16; x++)
            for (var z = 0; z < 16; z++)
            {
                chunk.SetBlockState(x, y, z, state.Value);
                oceanFloor.Update(x, y, z);
                worldSurface.Update(x, y, z);
            }
        }
    }

    //GetBaseHeight the highest opaque layer in the column, maps to vanilla getBaseHeight
    public override int GetBaseHeight(int x, int z, int type, LevelHeightAccessor level, RandomSource random)
    {
        var heightmapType = (NacreCraft.Registry.Heightmap.Types)type;
        for (var layerIndex = Math.Min(_layers.Count - 1, level.MaxBuildHeight - 1); layerIndex >= 0; layerIndex--)
        {
            var state = _layers[layerIndex];
            if (state is not null && Storage.LevelGen.Heightmap.IsOpaqueFor(heightmapType, state.Value))
                return level.MinBuildHeight + layerIndex + 1;
        }
        return level.MinBuildHeight;
    }

    //GetBaseColumn the base column sampled from the layer stack, maps to vanilla getBaseColumn
    public override object[] GetBaseColumn(int x, int z, LevelHeightAccessor level, RandomSource random)
    {
        var height = level.MaxBuildHeight - level.MinBuildHeight;
        var result = new object[height];
        for (var i = 0; i < height; i++)
            result[i] = i < _layers.Count ? _layers[i] ?? Blocks.AIR.DefaultBlockState : Blocks.AIR.DefaultBlockState;
        return result;
    }

    public override void BuildSurface(object region, object structures, ChunkAccess chunk, RandomSource random)
    {
    }

    public override void ApplyCarvers(long seed, ChunkAccess chunk, RandomSource random)
    {
    }

    public override void ApplyBiomeDecoration(WorldGenRegion region, ChunkAccess chunk, StructureFeatureManager structures)
    {
    }

    public override int GetMinY() => 0;

    public override int GetGenDepth() => 384;

    //GetSeaLevel sea level of the flat world, maps to vanilla getSeaLevel
    public int GetSeaLevel() => -63;
}
