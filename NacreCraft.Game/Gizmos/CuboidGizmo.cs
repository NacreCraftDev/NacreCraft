using NacreCraft.Primitives;
using NacreCraft.Registry.Environment;

namespace NacreCraft.Game.Gizmos;

//CuboidGizmo one axis-aligned box, maps to vanilla net.minecraft.gizmos.CuboidGizmo
public sealed record CuboidGizmo(AABB Box, GizmoStyle Style, bool ColoredCornerStroke) : Gizmo
{
    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
    {
        var x0 = Box.Min.X;
        var y0 = Box.Min.Y;
        var z0 = Box.Min.Z;
        var x1 = Box.Max.X;
        var y1 = Box.Max.Y;
        var z1 = Box.Max.Z;

        if (Style.HasFill())
        {
            var fill = Style.MultipliedFill(alphaMultiplier);
            primitives.AddQuad(new Vec3(x1, y0, z0), new Vec3(x1, y1, z0), new Vec3(x1, y1, z1), new Vec3(x1, y0, z1), fill);
            primitives.AddQuad(new Vec3(x0, y0, z0), new Vec3(x0, y0, z1), new Vec3(x0, y1, z1), new Vec3(x0, y1, z0), fill);
            primitives.AddQuad(new Vec3(x0, y0, z0), new Vec3(x0, y1, z0), new Vec3(x1, y1, z0), new Vec3(x1, y0, z0), fill);
            primitives.AddQuad(new Vec3(x0, y0, z1), new Vec3(x1, y0, z1), new Vec3(x1, y1, z1), new Vec3(x0, y1, z1), fill);
            primitives.AddQuad(new Vec3(x0, y1, z0), new Vec3(x0, y1, z1), new Vec3(x1, y1, z1), new Vec3(x1, y1, z0), fill);
            primitives.AddQuad(new Vec3(x0, y0, z0), new Vec3(x1, y0, z0), new Vec3(x1, y0, z1), new Vec3(x0, y0, z1), fill);
        }

        if (!Style.HasStroke()) return;

        var color = Style.MultipliedStroke(alphaMultiplier);
        primitives.AddLine(new Vec3(x0, y0, z0), new Vec3(x1, y0, z0), ColoredCornerStroke ? Argb.Multiply(color, -34953) : color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x0, y0, z0), new Vec3(x0, y1, z0), ColoredCornerStroke ? Argb.Multiply(color, -8913033) : color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x0, y0, z0), new Vec3(x0, y0, z1), ColoredCornerStroke ? Argb.Multiply(color, -8947713) : color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x1, y0, z0), new Vec3(x1, y1, z0), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x1, y1, z0), new Vec3(x0, y1, z0), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x0, y1, z0), new Vec3(x0, y1, z1), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x0, y1, z1), new Vec3(x0, y0, z1), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x0, y0, z1), new Vec3(x1, y0, z1), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x1, y0, z1), new Vec3(x1, y0, z0), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x0, y1, z1), new Vec3(x1, y1, z1), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x1, y0, z1), new Vec3(x1, y1, z1), color, Style.StrokeWidth);
        primitives.AddLine(new Vec3(x1, y1, z0), new Vec3(x1, y1, z1), color, Style.StrokeWidth);
    }
}
