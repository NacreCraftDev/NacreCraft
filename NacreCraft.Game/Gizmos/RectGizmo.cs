using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//RectGizmo one quad, maps to vanilla net.minecraft.gizmos.RectGizmo
public sealed record RectGizmo(Vec3 A, Vec3 B, Vec3 C, Vec3 D, GizmoStyle Style) : Gizmo
{
    public static RectGizmo FromCuboidFace(Vec3 cornerA, Vec3 cornerB, Direction face, GizmoStyle style)
        => face.Id3D switch
        {
            Direction.DownId => new RectGizmo(
                new Vec3(cornerA.X, cornerA.Y, cornerA.Z), new Vec3(cornerB.X, cornerA.Y, cornerA.Z),
                new Vec3(cornerB.X, cornerA.Y, cornerB.Z), new Vec3(cornerA.X, cornerA.Y, cornerB.Z), style),
            Direction.UpId => new RectGizmo(
                new Vec3(cornerA.X, cornerB.Y, cornerA.Z), new Vec3(cornerA.X, cornerB.Y, cornerB.Z),
                new Vec3(cornerB.X, cornerB.Y, cornerB.Z), new Vec3(cornerB.X, cornerB.Y, cornerA.Z), style),
            Direction.NorthId => new RectGizmo(
                new Vec3(cornerA.X, cornerA.Y, cornerA.Z), new Vec3(cornerA.X, cornerB.Y, cornerA.Z),
                new Vec3(cornerB.X, cornerB.Y, cornerA.Z), new Vec3(cornerB.X, cornerA.Y, cornerA.Z), style),
            Direction.SouthId => new RectGizmo(
                new Vec3(cornerA.X, cornerA.Y, cornerB.Z), new Vec3(cornerB.X, cornerA.Y, cornerB.Z),
                new Vec3(cornerB.X, cornerB.Y, cornerB.Z), new Vec3(cornerA.X, cornerB.Y, cornerB.Z), style),
            Direction.WestId => new RectGizmo(
                new Vec3(cornerA.X, cornerA.Y, cornerA.Z), new Vec3(cornerA.X, cornerA.Y, cornerB.Z),
                new Vec3(cornerA.X, cornerB.Y, cornerB.Z), new Vec3(cornerA.X, cornerB.Y, cornerA.Z), style),
            _ => new RectGizmo(
                new Vec3(cornerB.X, cornerA.Y, cornerA.Z), new Vec3(cornerB.X, cornerB.Y, cornerA.Z),
                new Vec3(cornerB.X, cornerB.Y, cornerB.Z), new Vec3(cornerB.X, cornerA.Y, cornerB.Z), style)
        };

    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
    {
        if (Style.HasFill())
            primitives.AddQuad(A, B, C, D, Style.MultipliedFill(alphaMultiplier));
        if (!Style.HasStroke()) return;

        var color = Style.MultipliedStroke(alphaMultiplier);
        primitives.AddLine(A, B, color, Style.StrokeWidth);
        primitives.AddLine(B, C, color, Style.StrokeWidth);
        primitives.AddLine(C, D, color, Style.StrokeWidth);
        primitives.AddLine(D, A, color, Style.StrokeWidth);
    }
}
