using NacreCraft.Client.Render.Model;

namespace NacreCraft.Client.Render.World;

//SectionMesh a compiled mesh holding 20-byte terrain vertices and int indices per RenderLayer
//Built from ChunkBuildBuffers by concatenating the facing groups in order; the facing ranges are only used
//while generating the indices, the finished mesh exposes just the flat vertex/index spans
//Vertices are section-local, the shader adds the region origin plus the section slot offset
public sealed class SectionMesh
{
    private const int LayerCount = 3;
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;

    //_vertexData/_indexData indexed by (int)RenderLayer: Solid=0 Cutout=1 Translucent=2
    //null means the layer has no vertices; GetVertices returns an empty span
    private readonly byte[]?[] _vertexData = new byte[LayerCount][];
    private readonly int[]?[] _indexData = new int[LayerCount][];
    private readonly int[] _vertexCounts = new int[LayerCount];

    public int TotalVertexCount { get; private set; }
    public int TotalIndexCount { get; private set; }

    public bool HasLayer(RenderLayer layer) => _vertexData[(int)layer] is not null;

    public ReadOnlySpan<byte> GetVertices(RenderLayer layer)
        => _vertexData[(int)layer] ?? ReadOnlySpan<byte>.Empty;

    public ReadOnlySpan<int> GetIndices(RenderLayer layer)
        => _indexData[(int)layer] ?? ReadOnlySpan<int>.Empty;

    public int GetVertexCount(RenderLayer layer) => _vertexCounts[(int)layer];

    //FromChunkBuildBuffers concatenates each layer's facing groups and generates quad indices per facing
    public static SectionMesh FromChunkBuildBuffers(ChunkBuildBuffers buffers)
    {
        var mesh = new SectionMesh();
        for (var layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            var layer = (RenderLayer)layerIndex;
            var segments = new VertexSegment[ChunkBuildBuffers.FacingCount];
            var totalBytes = 0;
            var vertexCount = 0;
            for (var facing = 0; facing < ChunkBuildBuffers.FacingCount; facing++)
            {
                var builder = buffers.Get(layer, facing);
                segments[facing] = new VertexSegment(vertexCount, builder.VertexCount);
                vertexCount += builder.VertexCount;
                totalBytes += builder.ByteLength;
            }
            if (vertexCount == 0) continue;
            var vertexData = new byte[totalBytes];
            var writeOffset = 0;
            for (var facing = 0; facing < ChunkBuildBuffers.FacingCount; facing++)
            {
                var builder = buffers.Get(layer, facing);
                if (builder.IsEmpty) continue;
                Array.Copy(builder.Buffer, 0, vertexData, writeOffset, builder.ByteLength);
                writeOffset += builder.ByteLength;
            }
            mesh._vertexData[layerIndex] = vertexData;
            mesh._vertexCounts[layerIndex] = vertexCount;
            mesh.TotalVertexCount += vertexCount;

            var indices = GenerateIndices(segments, vertexCount);
            mesh._indexData[layerIndex] = indices;
            mesh.TotalIndexCount += indices.Length;
        }
        return mesh;
    }

    //GenerateIndices emits 6 indices per quad, walking the facing groups so each group keeps its own base vertex
    private static int[] GenerateIndices(VertexSegment[] segments, int vertexCount)
    {
        var indices = new int[vertexCount / VerticesPerQuad * IndicesPerQuad];
        var cursor = 0;
        foreach (var segment in segments)
        {
            var quadCount = segment.VertexCount / VerticesPerQuad;
            for (var quad = 0; quad < quadCount; quad++)
            {
                var baseVertex = segment.VertexOffset + quad * VerticesPerQuad;
                indices[cursor + 0] = baseVertex + 0;
                indices[cursor + 1] = baseVertex + 1;
                indices[cursor + 2] = baseVertex + 2;
                indices[cursor + 3] = baseVertex + 2;
                indices[cursor + 4] = baseVertex + 3;
                indices[cursor + 5] = baseVertex + 0;
                cursor += IndicesPerQuad;
            }
        }
        return indices;
    }

    //VertexSegment the vertex range a facing group occupies inside a layer's concatenated vertex data
    private readonly record struct VertexSegment(int VertexOffset, int VertexCount);
}
