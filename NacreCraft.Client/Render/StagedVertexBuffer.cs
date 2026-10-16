using System.Numerics;
using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;

namespace NacreCraft.Client.Render;

//Draw vertex/index metadata for one draw call, maps to vanilla Draw
//Only records vertex layout and position without holding pipeline/texture/scissor; the upper GuiRenderer batches and groups
public sealed class Draw
{
    public required VertexFormat VertexFormat { get; init; }
    public PrimitiveTopology Topology { get; init; }
    //BaseVertex starting vertex offset in the vertex buffer, set to the accumulated vertex count in AppendDraw
    public int BaseVertex { get; internal set; }
    //VertexStartByte the Draw's byte offset in the frame's vertex buffer; draws are packed by their own stride, so
    //the byte offset cannot be derived from BaseVertex once two Draws of one frame use different vertex formats
    public int VertexStartByte { get; internal set; }
    //VertexCount vertex count incremented by VertexBuilder writes and locked after EndDraw
    public int VertexCount { get; internal set; }
    //QuadSorting sorts the draw's quads on write, null keeps the build order
    //Set through AppendDraw; only usable with the QUADS topology
    public VertexSorting? QuadSorting { get; internal set; }
    //Builder the BufferBuilder collecting this draw's vertices until EndDraw
    internal BufferBuilder? Builder { get; set; }
    //Mesh what EndDraw produced, null when the draw wrote no vertices
    internal MeshData? Mesh { get; set; }
    //FirstIndex starting index offset in the index buffer, auto-generated for QUADS
    public int FirstIndex { get; internal set; }
    //IndexCount index count, vertexCount/4*6 for QUADS and 0 otherwise
    public int IndexCount { get; internal set; }
    //Ended whether EndDraw was called, preventing a repeated lock
    internal bool Ended { get; set; }
}

//ExecuteInfo draw execution info passed to IRenderPass in the GPU submission phase
//VertexBuffer/IndexBuffer null means not uploaded this frame or no indices
public sealed record ExecuteInfo(GpuBuffer VertexBuffer, GpuBuffer? IndexBuffer, int BaseVertex, int FirstIndex, int IndexCount);

//StagedVertexBuffer staged vertex buffer, maps to vanilla StagedVertexBuffer
//The submission phase stages vertices via AppendDraw+GetVertexBuilder; EndDraw auto-generates QUADS indices
//The Upload phase stitches all Draw vertices into the vertex buffer + indices into the index buffer and uploads to the GPU
//The CPU staging logic is standalone and unit-testable; Upload depends on GpuDevice and only integration tests use real Vulkan
public sealed class StagedVertexBuffer : IDisposable
{
    //InitialCapacity starting size of one draw's vertex arena, it grows on demand
    private const int InitialCapacity = 4096;
    private readonly List<Draw> _draws = new();
    //_arenas one vertex arena per draw, handed out in draw order and kept across frames
    //A draw must own its arena because ByteBufferBuilder.Build only wraps the bytes written since the previous build,
    //so a shared arena would let the frame's first draw swallow every later draw's vertices
    private readonly List<ByteBufferBuilder> _arenas = new();
    //_arenasInUse arenas handed out this frame
    private int _arenasInUse;
    private readonly AutoStorageIndexBuffer _indexBuffer = new();
    private GpuBuffer? _vertexBuffer;
    private int _totalVertexCount;
    private int _totalVertexBytes;
    private bool _disposed;

    //Draws the added Draw list for the upper layer to iterate and submit
    public IReadOnlyList<Draw> Draws => _draws;

    //TotalVertexCount the frame's accumulated vertex count for capacity estimation
    public int TotalVertexCount => _totalVertexCount;

    //AppendDraw starts a new Draw; BaseVertex is filled in by EndDraw because a frame appends every draw
    //before any of them ends, so the running total is still zero here
    //Passing quadSorting reorders the draw's quads back to front when the draw ends, which the translucent pass needs
    public Draw AppendDraw(VertexFormat format, PrimitiveTopology topology, VertexSorting? quadSorting = null)
    {
        var draw = new Draw
        {
            VertexFormat = format,
            Topology = topology,
            QuadSorting = quadSorting,
            Builder = new BufferBuilder(RentArena(), topology, format)
        };
        _draws.Add(draw);
        return draw;
    }

    //RentArena hands out the arena of the next draw, opening a new one once the frame needs more than the previous one
    private ByteBufferBuilder RentArena()
    {
        if (_arenasInUse == _arenas.Count) _arenas.Add(new ByteBufferBuilder(InitialCapacity));
        return _arenas[_arenasInUse++];
    }

    //GetVertexBuilder returns the Draw's vertex consumer, writing in VertexFormat element order
    //Multiple elements in one Draw may use different poses without affecting batching; the pose is baked into the vertex position here
    public VertexConsumer GetVertexBuilder(Draw draw)
    {
        if (!_draws.Contains(draw)) throw new ArgumentException("Draw does not belong to this StagedVertexBuffer", nameof(draw));
        return draw.Builder ?? throw new InvalidOperationException("The draw has already ended");
    }

    //EndDraw locks the Draw's VertexCount and auto-generates QUADS indices into AutoStorageIndexBuffer
    //VertexBuilder writes already incremented draw.VertexCount; here it only accumulates globally and generates indices
    //Repeated calls are safe; the Ended flag prevents a repeated lock
    public void EndDraw(Draw draw)
    {
        if (draw.Ended) return;
        var builder = draw.Builder ?? throw new InvalidOperationException("The draw has no builder");
        draw.VertexCount = builder.VertexCount;
        draw.Mesh = builder.Build();
        //Quads are reordered before the indices are generated, so the shared QUADS pattern still applies
        if (draw.QuadSorting != null && draw.Topology == PrimitiveTopology.Quads)
            ReorderQuads(draw);
        //BaseVertex is assigned here rather than at AppendDraw time: every draw of a frame is appended before any
        //of them ends, so the running total is still zero then and every draw would claim vertex 0
        draw.BaseVertex = _totalVertexCount;
        _totalVertexCount += draw.VertexCount;
        draw.VertexStartByte = _totalVertexBytes;
        _totalVertexBytes += draw.VertexCount * draw.VertexFormat.VertexSize;
        if (draw.Topology == PrimitiveTopology.Quads)
        {
            var (firstIndex, indexCount) = _indexBuffer.Append(draw.VertexCount, draw.Topology);
            draw.FirstIndex = firstIndex;
            draw.IndexCount = indexCount;
        }
        draw.Ended = true;
    }

    //ReorderQuads rewrites the draw's vertex range so its quads come out back to front
    //Vanilla rebuilds an index buffer for this; reordering the vertices has the same effect and keeps the shared
    //QUADS index pattern usable, at the cost of one copy of the draw's own bytes
    private static void ReorderQuads(Draw draw)
    {
        var mesh = draw.Mesh;
        if (mesh is null) return;
        var quadCount = draw.VertexCount / 4;
        if (quadCount <= 1) return;
        var format = draw.VertexFormat;
        var position = format.GetElement(DefaultVertexFormat.PositionSemanticName)
            ?? throw new InvalidOperationException("Quad sorting needs a position element");
        if (position.Format != GpuFormat.Rgb32Float)
            throw new InvalidOperationException($"Quad sorting needs a 3 float position, got {position.Format}");

        var stride = format.VertexSize;
        var quadBytes = 4 * stride;
        var span = mesh.VertexBuffer.AsSpan();
        var centroids = new CompactVectorArray(quadCount);
        for (var quad = 0; quad < quadCount; quad++)
        {
            var first = quad * quadBytes + position.Offset;
            var third = first + 2 * stride;
            centroids.Set(quad,
                (System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span[first..]) +
                 System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span[third..])) * 0.5f,
                (System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span[(first + 4)..]) +
                 System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span[(third + 4)..])) * 0.5f,
                (System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span[(first + 8)..]) +
                 System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span[(third + 8)..])) * 0.5f);
        }

        var order = draw.QuadSorting!.Sort(centroids);
        var reordered = new byte[quadCount * quadBytes];
        for (var i = 0; i < quadCount; i++)
            span.Slice(order[i] * quadBytes, quadBytes)
                .CopyTo(reordered.AsSpan(i * quadBytes));
        reordered.CopyTo(span);
    }

    //Upload stitches all Draw vertices into the vertex buffer and indices into the index buffer, then uploads to the GPU
    //The buffer is reused across frames and rebuilt only when too small; host-visible, map+memcpy rewrites it each frame
    //Fixed the old bug where _vertexBuffer!=null skipped Upload, leaving the second frame's vertex data unupdated
    public void Upload(GpuDevice device)
    {
        var byteCount = 0;
        foreach (var draw in _draws)
            if (draw.Mesh is not null) byteCount += draw.Mesh.VertexBuffer.Size;
        if (byteCount == 0)
        {
            _indexBuffer.Upload(device);
            return;
        }
        if (_vertexBuffer == null || _vertexBuffer.Size < byteCount)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = device.CreateHostVisibleBuffer(byteCount, GpuBuffer.UsageVertex | GpuBuffer.UsageCopyDst);
        }
        //Each arena holds exactly one draw's vertices, so stitching the meshes in draw order reproduces the layout
        var combined = new byte[byteCount];
        var offset = 0;
        foreach (var draw in _draws)
        {
            if (draw.Mesh is null) continue;
            var span = draw.Mesh.VertexBuffer.AsSpan();
            span.CopyTo(combined.AsSpan(offset));
            offset += span.Length;
        }
        _vertexBuffer.Upload<byte>(combined);
        _indexBuffer.Upload(device);
    }

    //GetExecuteInfo returns the Draw's execution info, passed to IRenderPass in the submission phase
    public ExecuteInfo GetExecuteInfo(Draw draw)
    {
        return new ExecuteInfo(_vertexBuffer!, _indexBuffer.IndexBuffer, draw.BaseVertex, draw.FirstIndex, draw.IndexCount);
    }

    //EndFrame resets the staging area while keeping GPU buffers for cross-frame reuse
    public void EndFrame()
    {
        //Every mesh holds a view into its arena, so they are released before the arenas can be reused
        foreach (var draw in _draws) draw.Mesh?.Dispose();
        _draws.Clear();
        for (var i = 0; i < _arenasInUse; i++) _arenas[i].Clear();
        _arenasInUse = 0;
        _indexBuffer.EndFrame();
        _totalVertexCount = 0;
        _totalVertexBytes = 0;
    }

    //GetVertexBytes returns the Draw's vertex byte snapshot for unit tests to verify the layout
    public byte[] GetVertexBytes(Draw draw)
        => draw.Mesh is null ? Array.Empty<byte>() : draw.Mesh.VertexBuffer.AsSpan().ToArray();

    //IndexBuffer exposes the index buffer for unit tests to verify QUADS auto-indexing
    internal AutoStorageIndexBuffer IndexBuffer => _indexBuffer;

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var draw in _draws) draw.Mesh?.Dispose();
        _draws.Clear();
        foreach (var arena in _arenas) arena.Dispose();
        _arenas.Clear();
        _arenasInUse = 0;
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        _indexBuffer.Dispose();
        _disposed = true;
    }

}
