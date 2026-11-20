using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//CircleGizmo one horizontal circle, maps to vanilla net.minecraft.gizmos.CircleGizmo
public sealed record CircleGizmo(Vec3 Position, float Radius, GizmoStyle Style) : Gizmo
{
    private const int CircleVertices = 20;
    private const float SegmentSizeRadians = 0.31415927f;

    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
    {
        if (!Style.HasStroke() && !Style.HasFill()) return;

        var points = new Vec3[CircleVertices + 1];
        for (var i = 0; i < CircleVertices; i++)
        {
            var theta = i * SegmentSizeRadians;
            points[i] = Position.Add(
                (float)(Radius * Math.Cos(theta)), 0.0,
                (float)(Radius * Math.Sin(theta)));
        }
        points[CircleVertices] = points[0];

        if (Style.HasFill())
            primitives.AddTriangleFan(points, Style.MultipliedFill(alphaMultiplier));
        if (Style.HasStroke())
        {
            var color = Style.MultipliedStroke(alphaMultiplier);
            for (var i = 0; i < CircleVertices; i++)
                primitives.AddLine(points[i], points[i + 1], color, Style.StrokeWidth);
        }
    }
}
