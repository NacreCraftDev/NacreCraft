using System.Buffers.Binary;
using NacreCraft.Client.Blaze3d.Pipeline;

namespace NacreCraft.Client.Blaze3d.Vertex;

//MeshData the output of one build: a vertex view, the draw state describing how to draw it and an optional index view
//maps to vanilla com.mojang.blaze3d.vertex.MeshData
//Quads can be re-sorted into a separate index buffer, which is what the translucent pass needs back to front
public sealed unsafe class MeshData : IDisposable
{
    //QuadIndices the two triangles of one quad, in winding order
    private static readonly int[] s_quadIndices = { 0, 1, 2, 2, 3, 0 };

    private readonly ByteBufferBuilder.Result _vertexBuffer;
    private ByteBufferBuilder.Result? _indexBuffer;
    private bool _disposed;

    public MeshData(ByteBufferBuilder.Result vertexBuffer, DrawState drawState)
    {
        _vertexBuffer = vertexBuffer;
        State = drawState;
    }

    public DrawState State { get; }

    //VertexBuffer the vertex bytes written by the builder
    public ByteBufferBuilder.Result VertexBuffer => _vertexBuffer;

    //IndexBuffer the sorted index view, null until SortQuads runs
    public ByteBufferBuilder.Result? IndexBuffer => _indexBuffer;

    //SortQuads writes a quad-order index buffer into target, drawing far quads first
    //The vertices are left untouched; only the submission order changes
    public void SortQuads(ByteBufferBuilder target, VertexSorting sorting)
    {
        if (State.Topology != PrimitiveTopology.Quads)
            throw new InvalidOperationException($"Quad sorting requires QUADS topology, got {State.Topology}");
        var quadCount = State.VertexCount / 4;
        if (quadCount == 0) return;

        var centroids = new CompactVectorArray(quadCount);
        DecodeQuadCentroids(_vertexBuffer.AsSpan(), State.Format, centroids, State.VertexCount);
        var order = sorting.Sort(centroids);

        var indexSize = State.IndexType == IndexType.Short ? sizeof(ushort) : sizeof(int);
        var byteCount = quadCount * s_quadIndices.Length * indexSize;
        var span = new Span<byte>((void*)target.Reserve(byteCount), byteCount);
        var cursor = 0;
        foreach (var quad in order)
        {
            var baseVertex = quad * 4;
            foreach (var corner in s_quadIndices)
            {
                var vertex = baseVertex + corner;
                if (State.IndexType == IndexType.Short)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(span[cursor..], (ushort)vertex);
                    cursor += sizeof(ushort);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(span[cursor..], vertex);
                    cursor += sizeof(int);
                }
            }
        }

        _indexBuffer?.Dispose();
        _indexBuffer = target.Build();
    }

    //DecodeQuadCentroids averages the first and third vertex of each quad, maps to vanilla decodeQuadCentroids
    //Only a 3 float position element can be averaged, which is what every built-in format declares
    private static void DecodeQuadCentroids(ReadOnlySpan<byte> vertices, VertexFormat format, CompactVectorArray output, int vertexCount)
    {
        var position = format.GetElement(DefaultVertexFormat.PositionSemanticName)
            ?? throw new InvalidOperationException("Cannot decode quad centroids without a position element");
        if (position.Format != GpuFormat.Rgb32Float)
            throw new InvalidOperationException($"Quad centroid decoding needs a 3 float position, got {position.Format}");

        var stride = format.VertexSize;
        var quadCount = vertexCount / 4;
        for (var quad = 0; quad < quadCount; quad++)
        {
            var first = vertices[(quad * 4 * stride + position.Offset)..];
            var third = vertices[((quad * 4 + 2) * stride + position.Offset)..];
            var x = (BinaryPrimitives.ReadSingleLittleEndian(first) + BinaryPrimitives.ReadSingleLittleEndian(third)) * 0.5f;
            var y = (BinaryPrimitives.ReadSingleLittleEndian(first[4..]) + BinaryPrimitives.ReadSingleLittleEndian(third[4..])) * 0.5f;
            var z = (BinaryPrimitives.ReadSingleLittleEndian(first[8..]) + BinaryPrimitives.ReadSingleLittleEndian(third[8..])) * 0.5f;
            output.Set(quad, x, y, z);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _indexBuffer?.Dispose();
        _vertexBuffer.Dispose();
        _disposed = true;
    }

    //DrawState how to submit the data, maps to vanilla MeshData.DrawState
    public readonly record struct DrawState(
        VertexFormat Format,
        int VertexCount,
        int IndexCount,
        PrimitiveTopology Topology,
        IndexType IndexType);
}
