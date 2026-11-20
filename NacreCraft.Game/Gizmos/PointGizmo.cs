using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//PointGizmo one point, maps to vanilla net.minecraft.gizmos.PointGizmo
public sealed record PointGizmo(Vec3 Position, int Color, float Size) : Gizmo
{
    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
        => primitives.AddPoint(Position, GizmoColors.MultiplyAlpha(Color, alphaMultiplier), Size);
}
