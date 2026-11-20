using NacreCraft.Registry.Environment;

namespace NacreCraft.Game.Gizmos;

//GizmoStyle stroke and fill of one gizmo, maps to vanilla net.minecraft.gizmos.GizmoStyle
public sealed record GizmoStyle(int StrokeColor, float StrokeWidth, int FillColor)
{
    private const float DefaultWidth = 2.5f;

    public static GizmoStyle Stroke(int argb) => new(argb, DefaultWidth, 0);

    public static GizmoStyle Stroke(int argb, float width) => new(argb, width, 0);

    public static GizmoStyle Fill(int argb) => new(0, 0.0f, argb);

    public static GizmoStyle StrokeAndFill(int stroke, float strokeWidth, int fill)
        => new(stroke, strokeWidth, fill);

    public bool HasFill() => FillColor != 0;

    public bool HasStroke() => StrokeColor != 0 && StrokeWidth > 0.0f;

    public int MultipliedStroke(float alphaMultiplier)
        => GizmoColors.MultiplyAlpha(StrokeColor, alphaMultiplier);

    public int MultipliedFill(float alphaMultiplier)
        => GizmoColors.MultiplyAlpha(FillColor, alphaMultiplier);
}

//GizmoColors alpha helpers shared by the gizmo shapes
internal static class GizmoColors
{
    public static int MultiplyAlpha(int color, float multiplier)
        => Argb.Color((int)(Argb.Alpha(color) * multiplier), color & 0xFFFFFF);
}
