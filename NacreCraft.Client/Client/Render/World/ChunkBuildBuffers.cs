using NacreCraft.Client.Render.Model;

namespace NacreCraft.Client.Render.World;

//ChunkBuildBuffers the per-layer per-facing vertex buffers of one build, maps to Sodium's ChunkBuildBuffers
//Splitting by facing keeps the door open for rebuilding an index buffer over a subset of facings without
//touching vertex data, which the translucent pass and the region batch builder both need
public sealed class ChunkBuildBuffers
{
    public const int LayerCount = 3;
    public const int FacingCount = 7;
    //UnassignedFacing group index for quads whose facing carries no direction information
    public const int UnassignedFacing = 6;

    private readonly ChunkVertexBufferBuilder[,] _builders = new ChunkVertexBufferBuilder[LayerCount, FacingCount];

    public ChunkBuildBuffers()
    {
        for (var layer = 0; layer < LayerCount; layer++)
        for (var facing = 0; facing < FacingCount; facing++)
            _builders[layer, facing] = new ChunkVertexBufferBuilder();
    }

    public ChunkVertexBufferBuilder Get(RenderLayer layer, Direction direction)
        => _builders[(int)layer, (int)direction];

    public ChunkVertexBufferBuilder Get(RenderLayer layer, int facing)
        => _builders[(int)layer, facing];

    //FacingOf maps a quad facing to its group index
    public static int FacingOf(Direction? direction)
        => direction is null ? UnassignedFacing : (int)direction.Value;

    public int TotalVertexCount
    {
        get
        {
            var sum = 0;
            for (var layer = 0; layer < LayerCount; layer++)
            for (var facing = 0; facing < FacingCount; facing++)
                sum += _builders[layer, facing].VertexCount;
            return sum;
        }
    }

    public void Clear()
    {
        for (var layer = 0; layer < LayerCount; layer++)
        for (var facing = 0; facing < FacingCount; facing++)
            _builders[layer, facing].Clear();
    }
}
