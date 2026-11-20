using System.Numerics;
using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//ArrowGizmo one arrow, maps to vanilla net.minecraft.gizmos.ArrowGizmo
public sealed record ArrowGizmo(Vec3 Start, Vec3 End, int Color, float Width) : Gizmo
{
    public const float DefaultWidth = 2.5f;

    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
    {
        var color = GizmoColors.MultiplyAlpha(Color, alphaMultiplier);
        primitives.AddLine(Start, End, color, Width);

        var delta = End.Subtract(Start);
        var direction = delta.Normalize();
        var rotation = RotationTo(Vector3.UnitX, new Vector3((float)direction.X, (float)direction.Y, (float)direction.Z));
        var len = (float)Math.Clamp(End.DistanceTo(Start) * 0.1, 0.1, 1.0);
        var tips = new[]
        {
            Vector3.Transform(new Vector3(-len, len, 0.0f), rotation),
            Vector3.Transform(new Vector3(-len, 0.0f, len), rotation),
            Vector3.Transform(new Vector3(-len, -len, 0.0f), rotation),
            Vector3.Transform(new Vector3(-len, 0.0f, -len), rotation)
        };
        foreach (var tip in tips)
            primitives.AddLine(End.Add(tip.X, tip.Y, tip.Z), End, color, Width);
    }

    private static Quaternion RotationTo(Vector3 from, Vector3 to)
    {
        var cross = Vector3.Cross(from, to);
        var dot = Vector3.Dot(from, to);
        if (dot < -0.999999f)
            return Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        return Quaternion.Normalize(new Quaternion(cross, 1.0f + dot));
    }
}
