using NacreCraft.Primitives;
using NacreCraft.Registry;

namespace NacreCraft.Game.Gizmos;

//Gizmos the static entry points for creating debug gizmos, maps to vanilla net.minecraft.gizmos.Gizmos
public static class Gizmos
{
    private static readonly ThreadLocal<GizmoCollector?> Collector = new();

    public static TemporaryCollection WithCollector(GizmoCollector collector)
    {
        var result = new TemporaryCollection();
        Collector.Value = collector;
        return result;
    }

    public static GizmoProperties AddGizmo(Gizmo gizmo)
    {
        var collector = Collector.Value
            ?? throw new InvalidOperationException("Gizmos cannot be created here! No GizmoCollector has been registered.");
        return collector.Add(gizmo);
    }

    public static GizmoProperties Cuboid(AABB box, GizmoStyle style) => Cuboid(box, style, false);

    public static GizmoProperties Cuboid(AABB box, GizmoStyle style, bool coloredCorner)
        => AddGizmo(new CuboidGizmo(box, style, coloredCorner));

    public static GizmoProperties Cuboid(BlockPos pos, GizmoStyle style)
        => Cuboid(new AABB(pos.X, pos.Y, pos.Z, pos.X + 1.0, pos.Y + 1.0, pos.Z + 1.0), style);

    public static GizmoProperties Cuboid(BlockPos pos, float padding, GizmoStyle style)
        => Cuboid(new AABB(pos.X, pos.Y, pos.Z, pos.X + 1.0, pos.Y + 1.0, pos.Z + 1.0)
            .Inflate(padding, padding, padding), style);

    public static GizmoProperties Circle(Vec3 pos, float radius, GizmoStyle style)
        => AddGizmo(new CircleGizmo(pos, radius, style));

    public static GizmoProperties Line(Vec3 start, Vec3 end, int argb)
        => AddGizmo(new LineGizmo(start, end, argb, LineGizmo.DefaultWidth));

    public static GizmoProperties Line(Vec3 start, Vec3 end, int argb, float width)
        => AddGizmo(new LineGizmo(start, end, argb, width));

    public static GizmoProperties Arrow(Vec3 start, Vec3 end, int argb)
        => AddGizmo(new ArrowGizmo(start, end, argb, ArrowGizmo.DefaultWidth));

    public static GizmoProperties Arrow(Vec3 start, Vec3 end, int argb, float width)
        => AddGizmo(new ArrowGizmo(start, end, argb, width));

    public static GizmoProperties Rect(Vec3 cornerA, Vec3 cornerB, Direction face, GizmoStyle style)
        => AddGizmo(RectGizmo.FromCuboidFace(cornerA, cornerB, face, style));

    public static GizmoProperties Rect(Vec3 cornerA, Vec3 cornerB, Vec3 cornerC, Vec3 cornerD, GizmoStyle style)
        => AddGizmo(new RectGizmo(cornerA, cornerB, cornerC, cornerD, style));

    public static GizmoProperties Point(Vec3 position, int argb, float size)
        => AddGizmo(new PointGizmo(position, argb, size));

    public static GizmoProperties BillboardTextOverBlock(string text, BlockPos pos, int row, int color, float scale)
    {
        var properties = BillboardText(text,
            new Vec3(pos.X + 0.5, pos.Y + 1.3 + row * 0.2, pos.Z + 0.5),
            TextGizmo.Style.ForColorAndCentered(color).WithScale(scale));
        properties.SetAlwaysOnTop();
        return properties;
    }

    public static GizmoProperties BillboardTextOverMob(Entity entity, int row, string text, int color, float scale)
    {
        var block = entity.BlockPosition();
        var properties = BillboardText(text,
            new Vec3(block.X + 0.5, entity.Pos.Y + 2.4 + row * 0.25, block.Z + 0.5),
            TextGizmo.Style.ForColor(color).WithScale(scale).WithLeftAlignment(0.5f));
        properties.SetAlwaysOnTop();
        return properties;
    }

    public static GizmoProperties BillboardText(string text, Vec3 pos, TextGizmo.Style style)
        => AddGizmo(new TextGizmo(pos, text, style));

    //TemporaryCollection restores the previous collector when disposed
    public sealed class TemporaryCollection : IDisposable
    {
        private readonly GizmoCollector? _old = Collector.Value;
        private bool _closed;

        internal TemporaryCollection()
        {
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            Collector.Value = _old;
        }
    }
}
