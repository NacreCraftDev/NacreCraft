using System.Numerics;

namespace NacreCraft.Client.Blaze3d.Vertex;

//VertexConsumer the vertex write interface, maps to vanilla com.mojang.blaze3d.vertex.VertexConsumer
//Callers write attribute by attribute; the concrete consumer decides where and in what layout the bytes land
//BufferBuilder is the implementation that packs them into a ByteBufferBuilder according to a VertexFormat
public interface VertexConsumer
{
    //AddVertex finishes the current vertex at the given position; every attribute set since the previous call belongs to it
    void AddVertex(float x, float y, float z);

    void SetColor(int red, int green, int blue, int alpha);

    void SetUv(float u, float v);

    //SetUv1 the packed overlay coordinates, maps to vanilla setUv1
    void SetUv1(int u, int v);

    //SetUv2 the packed light coordinates, maps to vanilla setUv2
    void SetUv2(int u, int v);

    void SetNormal(float x, float y, float z);

    void SetLineWidth(float width);

    //SetColor from a packed ARGB color
    void SetColor(int argb)
        => SetColor((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF, (argb >> 24) & 0xFF);

    //SetColor from normalized components
    void SetColor(float red, float green, float blue, float alpha)
        => SetColor((int)(red * 255f), (int)(green * 255f), (int)(blue * 255f), (int)(alpha * 255f));

    //SetOverlay the overlay coordinates packed into the low and high halves of one int
    void SetOverlay(int packedOverlay)
        => SetUv1(packedOverlay & 0xFFFF, (packedOverlay >> 16) & 0xFFFF);

    //SetLight the light coordinates packed into the low and high halves of one int
    void SetLight(int packedLight)
        => SetUv2(packedLight & 0xFFFF, (packedLight >> 16) & 0xFFFF);

    //AddVertexWith2DPose finishes a vertex whose position is transformed by a 2D pose, maps to vanilla addVertexWith2DPose
    //The pose is baked into the position here, so several elements using different poses still batch into one draw
    void AddVertexWith2DPose(Matrix3x2 pose, float x, float y, float u, float v, int color)
    {
        var transformed = Vector2.Transform(new Vector2(x, y), pose);
        SetColor(color);
        SetUv(u, v);
        AddVertex(transformed.X, transformed.Y, 0f);
    }

    //AddVertex with every attribute at once, maps to the vanilla convenience overload
    void AddVertex(float x, float y, float z, int color, float u, float v, int overlay, int light, float nx, float ny, float nz)
    {
        SetColor(color);
        SetUv(u, v);
        SetOverlay(overlay);
        SetLight(light);
        SetNormal(nx, ny, nz);
        AddVertex(x, y, z);
    }
}
