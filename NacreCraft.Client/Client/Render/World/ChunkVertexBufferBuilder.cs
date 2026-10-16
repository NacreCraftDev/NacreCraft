using System.Buffers.Binary;
using System.Numerics;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Model;

namespace NacreCraft.Client.Render.World;

//ChunkVertexBufferBuilder accumulates the 20-byte terrain vertices of one layer+facing group
//maps to Sodium's VertexBufferBuilder
//The backing array doubles on growth; EncodeQuad writes all four vertices at once because the texture
//quantization needs the sprite centroid of the whole quad
public sealed class ChunkVertexBufferBuilder
{
    private const int InitialCapacity = 1024;

    private byte[] _buffer = new byte[InitialCapacity];
    private int _length;

    public byte[] Buffer => _buffer;
    public int ByteLength => _length;
    public int VertexCount { get; private set; }
    public bool IsEmpty => _length == 0;

    public void Clear()
    {
        _length = 0;
        VertexCount = 0;
    }

    //EncodeQuad transforms the quad with the pose and appends its four vertices
    //materialBits is the packed material flag byte; sectionIndex is the region-local section slot, 0 while there is no region yet
    public void EncodeQuad(PoseStack poseStack, in BakedQuad quad, QuadInstance instance, int materialBits, int sectionIndex)
    {
        var pose = poseStack.Pose();
        //The texture quantization shrinks each corner toward the quad centroid, so the centroid is needed first
        var centerU = (quad.Uv0.X + quad.Uv1.X + quad.Uv2.X + quad.Uv3.X) * 0.25f;
        var centerV = (quad.Uv0.Y + quad.Uv1.Y + quad.Uv2.Y + quad.Uv3.Y) * 0.25f;

        EnsureCapacity(ChunkVertexFormat.Stride * BakedQuad.VertexCount);
        var dest = _buffer.AsSpan(_length);
        var lightEmission = quad.LightEmission;
        for (var i = 0; i < BakedQuad.VertexCount; i++)
        {
            var pos = Vector3.Transform(quad.Position(i), pose);
            var uv = quad.Uv(i);
            var light = instance.GetLightCoordsWithEmission(i, lightEmission);

            var x = ChunkVertexFormat.QuantizePosition(pos.X);
            var y = ChunkVertexFormat.QuantizePosition(pos.Y);
            var z = ChunkVertexFormat.QuantizePosition(pos.Z);
            var u = ChunkVertexFormat.EncodeTexture(centerU, uv.X);
            var v = ChunkVertexFormat.EncodeTexture(centerV, uv.Y);
            var encodedLight = ChunkVertexFormat.EncodeLight(
                LightTexture.UnpackBlockLight(light), LightTexture.UnpackSkyLight(light));

            var slot = dest.Slice(i * ChunkVertexFormat.Stride);
            BinaryPrimitives.WriteInt32LittleEndian(slot, ChunkVertexFormat.PackPositionHi(x, y, z));
            BinaryPrimitives.WriteInt32LittleEndian(slot[4..], ChunkVertexFormat.PackPositionLo(x, y, z));
            //RGBA8_UNORM reads the bytes in R,G,B,A order, while the instance color is packed ARGB
            BinaryPrimitives.WriteInt32LittleEndian(slot[8..], ToRgbaBytes(instance.GetColor(i)));
            BinaryPrimitives.WriteInt32LittleEndian(slot[12..], ChunkVertexFormat.PackTexture(u, v));
            BinaryPrimitives.WriteInt32LittleEndian(slot[16..], ChunkVertexFormat.PackLightAndData(encodedLight, materialBits, sectionIndex));
        }
        _length += ChunkVertexFormat.Stride * BakedQuad.VertexCount;
        VertexCount += BakedQuad.VertexCount;
    }

    //ToRgbaBytes swizzles a packed ARGB color into the little-endian RGBA byte order the attribute expects
    private static int ToRgbaBytes(int argb)
        => ((argb >> 16) & 0xFF)
            | (((argb >> 8) & 0xFF) << 8)
            | ((argb & 0xFF) << 16)
            | ((argb >> 24) & 0xFF) << 24;

    private void EnsureCapacity(int additionalBytes)
    {
        var required = _length + additionalBytes;
        if (required <= _buffer.Length) return;
        var size = _buffer.Length;
        while (size < required) size *= 2;
        Array.Resize(ref _buffer, size);
    }
}
