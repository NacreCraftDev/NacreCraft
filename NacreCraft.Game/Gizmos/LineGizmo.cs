using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//LineGizmo one line segment, maps to vanilla net.minecraft.gizmos.LineGizmo
public sealed record LineGizmo(Vec3 Start, Vec3 End, int Color, float Width) : Gizmo
{
    public const float DefaultWidth = 3.0f;

    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
        => primitives.AddLine(Start, End, GizmoColors.MultiplyAlpha(Color, alphaMultiplier), Width);
}
