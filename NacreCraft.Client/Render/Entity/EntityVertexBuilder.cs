using System.Numerics;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Model;

namespace NacreCraft.Client.Render.Entity;

//EntityVertexBuilder writes entity quads into the active RenderType vertex consumer
//ModelPart compiles models through AddQuad; the dispatcher selects the consumer with Begin before each entity
public sealed class EntityVertexBuilder
{
    private VertexConsumer? _target;

    //VertexCount vertices written since Begin
    public int VertexCount { get; private set; }

    //Begin points the builder at the consumer of one batch's render type
    public void Begin(VertexConsumer target)
    {
        _target = target;
        VertexCount = 0;
    }

    //Clear detaches the consumer so a late write fails loudly instead of going to the previous entity
    public void Clear()
    {
        _target = null;
        VertexCount = 0;
    }

    //AddVertex writes one vertex with packed color/overlay/light, returning its index within this batch
    public int AddVertex(Vector3 pos, int color, float u, float v, int overlay, int light, Vector3 normal)
    {
        var target = _target ?? throw new InvalidOperationException("EntityVertexBuilder.Begin must be called before writing vertices");
        target.SetColor(color);
        target.SetUv(u, v);
        target.SetOverlay(overlay);
        target.SetLight(light);
        target.SetNormal(normal.X, normal.Y, normal.Z);
        target.AddVertex(pos.X, pos.Y, pos.Z);
        return VertexCount++;
    }

    //AddQuad writes four vertices; the staged buffer generates the quad indices on upload
    public void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        int color, float u0, float v0, float u1, float v1,
        int overlay, int light, Vector3 normal)
    {
        AddVertex(p0, color, u0, v0, overlay, light, normal);
        AddVertex(p1, color, u1, v0, overlay, light, normal);
        AddVertex(p2, color, u1, v1, overlay, light, normal);
        AddVertex(p3, color, u0, v1, overlay, light, normal);
    }

    //AddQuad writes a baked model quad with per-vertex UVs
    public void AddQuad(in BakedQuad quad, Matrix4x4 pose, Matrix4x4 normalMatrix,
        int color, int overlay, int light)
    {
        var normal = Vector3.TransformNormal(quad.Direction.UnitVector(), normalMatrix);
        AddVertex(Vector3.Transform(quad.P0, pose), color, quad.Uv0.X, quad.Uv0.Y, overlay, light, normal);
        AddVertex(Vector3.Transform(quad.P1, pose), color, quad.Uv1.X, quad.Uv1.Y, overlay, light, normal);
        AddVertex(Vector3.Transform(quad.P2, pose), color, quad.Uv2.X, quad.Uv2.Y, overlay, light, normal);
        AddVertex(Vector3.Transform(quad.P3, pose), color, quad.Uv3.X, quad.Uv3.Y, overlay, light, normal);
    }
}
