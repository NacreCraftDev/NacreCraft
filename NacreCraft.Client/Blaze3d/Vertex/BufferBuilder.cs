using NacreCraft.Client.Blaze3d.Pipeline;

namespace NacreCraft.Client.Blaze3d.Vertex;

//BufferBuilder packs vertices described by a VertexFormat into a ByteBufferBuilder, maps to vanilla BufferBuilder
//Callers set attributes in any order and finish each vertex with AddVertex; an attribute the format does not
//declare is ignored, which is what lets one convenience call serve several formats
//Supported attribute layouts are the ones DefaultVertexFormat currently declares
public sealed unsafe class BufferBuilder : VertexConsumer
{
    private readonly ByteBufferBuilder _buffer;
    private readonly PrimitiveTopology _topology;
    private readonly VertexFormat _format;
    private readonly int _vertexSize;
    private readonly int _positionOffset;
    private readonly int _colorOffset;
    private readonly int _uv0Offset;
    private readonly int _uv1Offset;
    private readonly int _uv2Offset;
    private readonly int _normalOffset;
    private readonly int _lineWidthOffset;

    private nint _vertexPointer;
    private bool _building;
    private int _elementsToFill;
    private int _vertexCount;
    private bool _closed;

    public BufferBuilder(ByteBufferBuilder buffer, PrimitiveTopology topology, VertexFormat format)
    {
        if (!format.Contains(DefaultVertexFormat.PositionSemanticName))
            throw new ArgumentException("A vertex format must declare a Position element", nameof(format));
        _buffer = buffer;
        _topology = topology;
        _format = format;
        _vertexSize = format.VertexSize;
        _positionOffset = OffsetOf(format, DefaultVertexFormat.PositionSemanticName);
        _colorOffset = OffsetOf(format, DefaultVertexFormat.ColorSemanticName);
        _uv0Offset = OffsetOf(format, DefaultVertexFormat.Uv0SemanticName);
        _uv1Offset = OffsetOf(format, DefaultVertexFormat.Uv1SemanticName);
        _uv2Offset = OffsetOf(format, DefaultVertexFormat.Uv2SemanticName);
        _normalOffset = OffsetOf(format, DefaultVertexFormat.NormalSemanticName);
        _lineWidthOffset = OffsetOf(format, DefaultVertexFormat.LineWidthSemanticName);
    }

    //VertexCount vertices finished so far
    public int VertexCount => _vertexCount;

    public void AddVertex(float x, float y, float z)
    {
        //Attributes may already have started the vertex; only the position is still missing here
        if (!_building) BeginVertex();
        var position = (float*)BeginElement(_positionOffset);
        position[0] = x;
        position[1] = y;
        position[2] = z;
        EndVertex();
    }

    public void SetColor(int red, int green, int blue, int alpha)
    {
        if (!TryBeginElement(_colorOffset, out var pointer)) return;
        var color = (byte*)pointer;
        color[0] = (byte)red;
        color[1] = (byte)green;
        color[2] = (byte)blue;
        color[3] = (byte)alpha;
    }

    public void SetUv(float u, float v)
    {
        if (!TryBeginElement(_uv0Offset, out var pointer)) return;
        var uv = (float*)pointer;
        uv[0] = u;
        uv[1] = v;
    }

    public void SetUv1(int u, int v)
    {
        if (!TryBeginElement(_uv1Offset, out var pointer)) return;
        var uv = (short*)pointer;
        uv[0] = (short)u;
        uv[1] = (short)v;
    }

    public void SetUv2(int u, int v)
    {
        if (!TryBeginElement(_uv2Offset, out var pointer)) return;
        var uv = (short*)pointer;
        uv[0] = (short)u;
        uv[1] = (short)v;
    }

    public void SetNormal(float x, float y, float z)
    {
        if (!TryBeginElement(_normalOffset, out var pointer)) return;
        var normal = (float*)pointer;
        normal[0] = x;
        normal[1] = y;
        normal[2] = z;
    }

    public void SetLineWidth(float width)
    {
        if (!TryBeginElement(_lineWidthOffset, out var pointer)) return;
        *(float*)pointer = width;
    }

    //Build closes the builder and wraps what was written into MeshData, or returns null when nothing was written
    public MeshData? Build()
    {
        if (_building) throw new InvalidOperationException("Not enough elements were set before build");
        _closed = true;
        var result = _buffer.Build();
        if (result is null) return null;
        var indexType = _vertexCount > ushort.MaxValue ? IndexType.Int : IndexType.Short;
        var state = new MeshData.DrawState(_format, _vertexCount, IndexCountFor(_vertexCount), _topology, indexType);
        return new MeshData(result, state);
    }

    //IndexCountFor how many indices the topology expands the vertices into, maps to vanilla PrimitiveTopology.indexCount
    private int IndexCountFor(int vertexCount) => _topology switch
    {
        PrimitiveTopology.Quads => vertexCount / 4 * 6,
        _ => vertexCount
    };

    private void BeginVertex()
    {
        if (_closed) throw new ObjectDisposedException(nameof(BufferBuilder));
        if (_building) throw new InvalidOperationException("Not enough elements were set before AddVertex");
        _building = true;
        _vertexPointer = _buffer.Reserve(_vertexSize);
        _elementsToFill = _format.Elements.Count;
    }

    //TryBeginElement starts the vertex if needed and returns the address of one attribute slot
    //Returns false when the format does not declare the attribute, which vanilla ignores silently
    private bool TryBeginElement(int offset, out nint pointer)
    {
        if (offset < 0)
        {
            pointer = 0;
            return false;
        }
        if (!_building) BeginVertex();
        _elementsToFill--;
        pointer = _vertexPointer + offset;
        return true;
    }

    //BeginElement is the strict form used for the position, which every format must declare
    private nint BeginElement(int offset)
    {
        if (offset < 0) throw new InvalidOperationException("The vertex format does not declare this attribute");
        if (!_building) BeginVertex();
        _elementsToFill--;
        return _vertexPointer + offset;
    }

    private void EndVertex()
    {
        //Attributes the format declares but the caller never set would leave holes in the vertex
        if (_elementsToFill > 0)
            throw new InvalidOperationException($"{_elementsToFill} elements of the vertex were left unfilled");
        _building = false;
        _vertexCount++;
    }

    private static int OffsetOf(VertexFormat format, string name)
        => format.GetElement(name)?.Offset ?? -1;
}
