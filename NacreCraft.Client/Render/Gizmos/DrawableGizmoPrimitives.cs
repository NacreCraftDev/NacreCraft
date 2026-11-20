using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Rendertype;
using NacreCraft.Game.Gizmos;
using NacreCraft.Primitives;
using NacreCraft.Registry.Environment;

namespace NacreCraft.Client.Render.Gizmos;

//DrawableGizmoPrimitives collects gizmo primitives split into opaque and translucent groups, maps to vanilla
//net.minecraft.client.renderer.gizmos.DrawableGizmoPrimitives
//Vanilla submits the groups through SubmitNodeCollector; NC draws them directly because the feature/submit pipeline is
//not ported yet, so Text entries are collected but not rendered (the font TextRenderable path is still missing)
public sealed class DrawableGizmoPrimitives : GizmoPrimitives
{
    public Group OpaqueGroup { get; } = new(true);
    public Group TranslucentGroup { get; } = new(false);

    public bool IsEmpty { get; private set; } = true;

    private Group GetGroup(int color) => Argb.Alpha(color) < 255 ? TranslucentGroup : OpaqueGroup;

    public void AddPoint(Vec3 position, int color, float size)
    {
        GetGroup(color).Points.Add(new Point(position, color, size));
        IsEmpty = false;
    }

    public void AddLine(Vec3 start, Vec3 end, int color, float width)
    {
        GetGroup(color).Lines.Add(new Line(start, end, color, width));
        IsEmpty = false;
    }

    public void AddTriangleFan(Vec3[] points, int color)
    {
        GetGroup(color).TriangleFans.Add(new TriangleFan(points, color));
        IsEmpty = false;
    }

    public void AddQuad(Vec3 a, Vec3 b, Vec3 c, Vec3 d, int color)
    {
        GetGroup(color).Quads.Add(new Quad(a, b, c, d, color));
        IsEmpty = false;
    }

    public void AddText(Vec3 position, string text, TextGizmo.Style style)
    {
        GetGroup(style.Color).Texts.Add(new Text(position, text, style));
        IsEmpty = false;
    }

    //Draw writes both groups into the shared buffer source, opaque first
    public void Draw(MultiBufferSource.BufferSource source)
    {
        if (IsEmpty) return;
        DrawGroup(source, OpaqueGroup);
        DrawGroup(source, TranslucentGroup);
        IsEmpty = true;
        OpaqueGroup.Clear();
        TranslucentGroup.Clear();
    }

    private static void DrawGroup(MultiBufferSource.BufferSource source, Group group)
    {
        if (group.Quads.Count > 0)
        {
            var builder = source.GetBuffer(RenderTypes.DebugFilledBox());
            foreach (var quad in group.Quads)
            {
                WriteVertex(builder, quad.A, quad.Color);
                WriteVertex(builder, quad.B, quad.Color);
                WriteVertex(builder, quad.C, quad.Color);
                WriteVertex(builder, quad.D, quad.Color);
            }
        }

        if (group.TriangleFans.Count > 0)
        {
            var builder = source.GetBuffer(RenderTypes.DebugTriangleFan());
            foreach (var fan in group.TriangleFans)
            {
                foreach (var point in fan.Points)
                    WriteVertex(builder, point, fan.Color);
            }
        }

        if (group.Lines.Count > 0)
        {
            var builder = source.GetBuffer(group.Opaque ? RenderTypes.Lines() : RenderTypes.LinesTranslucent());
            foreach (var line in group.Lines)
            {
                var nx = (float)(line.End.X - line.Start.X);
                var ny = (float)(line.End.Y - line.Start.Y);
                var nz = (float)(line.End.Z - line.Start.Z);
                builder.SetColor(line.Color);
                builder.SetNormal(nx, ny, nz);
                builder.SetLineWidth(line.Width);
                builder.AddVertex((float)line.Start.X, (float)line.Start.Y, (float)line.Start.Z);
                builder.SetColor(line.Color);
                builder.SetNormal(nx, ny, nz);
                builder.SetLineWidth(line.Width);
                builder.AddVertex((float)line.End.X, (float)line.End.Y, (float)line.End.Z);
            }
        }

        if (group.Points.Count > 0)
        {
            var builder = source.GetBuffer(RenderTypes.DebugPoint());
            foreach (var point in group.Points)
            {
                builder.SetColor(point.Color);
                builder.SetLineWidth(point.Size);
                builder.AddVertex((float)point.Pos.X, (float)point.Pos.Y, (float)point.Pos.Z);
            }
        }
    }

    private static void WriteVertex(VertexConsumer builder, Vec3 position, int color)
    {
        builder.SetColor(color);
        builder.AddVertex((float)position.X, (float)position.Y, (float)position.Z);
    }

    public sealed record Group(bool Opaque, List<Line> Lines, List<Quad> Quads, List<TriangleFan> TriangleFans, List<Text> Texts, List<Point> Points)
    {
        public Group(bool opaque) : this(opaque, new List<Line>(), new List<Quad>(), new List<TriangleFan>(), new List<Text>(), new List<Point>())
        {
        }

        public void Clear()
        {
            Lines.Clear();
            Quads.Clear();
            TriangleFans.Clear();
            Texts.Clear();
            Points.Clear();
        }
    }

    public sealed record Point(Vec3 Pos, int Color, float Size);

    public sealed record Line(Vec3 Start, Vec3 End, int Color, float Width);

    public sealed record TriangleFan(Vec3[] Points, int Color);

    public sealed record Quad(Vec3 A, Vec3 B, Vec3 C, Vec3 D, int Color);

    public sealed record Text(Vec3 Pos, string TextValue, TextGizmo.Style Style);
}
