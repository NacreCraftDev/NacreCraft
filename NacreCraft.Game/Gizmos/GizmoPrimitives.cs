using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//GizmoPrimitives the primitive sink a gizmo emits into, maps to vanilla net.minecraft.gizmos.GizmoPrimitives
public interface GizmoPrimitives
{
    void AddPoint(Vec3 position, int color, float size);

    void AddLine(Vec3 start, Vec3 end, int color, float width);

    void AddTriangleFan(Vec3[] points, int color);

    void AddQuad(Vec3 a, Vec3 b, Vec3 c, Vec3 d, int color);

    void AddText(Vec3 position, string text, TextGizmo.Style style);
}
