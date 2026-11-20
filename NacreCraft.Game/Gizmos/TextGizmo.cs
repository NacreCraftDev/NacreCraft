using NacreCraft.Primitives;

namespace NacreCraft.Game.Gizmos;

//TextGizmo one billboard text label, maps to vanilla net.minecraft.gizmos.TextGizmo
public sealed record TextGizmo(Vec3 Position, string Text, TextGizmo.Style TextStyle) : Gizmo
{
    public void Emit(GizmoPrimitives primitives, float alphaMultiplier)
    {
        var style = alphaMultiplier < 1.0f
            ? new Style(GizmoColors.MultiplyAlpha(TextStyle.Color, alphaMultiplier), TextStyle.Scale, TextStyle.AdjustLeft)
            : TextStyle;
        primitives.AddText(Position, Text, style);
    }

    public sealed record Style(int Color, float Scale, double? AdjustLeft)
    {
        public const float DefaultScale = 0.32f;

        public static Style WhiteAndCentered() => new(-1, DefaultScale, null);

        public static Style ForColorAndCentered(int argb) => new(argb, DefaultScale, null);

        public static Style ForColor(int argb) => new(argb, DefaultScale, 0.0);

        public Style WithScale(float scale) => new(Color, scale, AdjustLeft);

        public Style WithLeftAlignment(float adjustLeft) => new(Color, Scale, adjustLeft);
    }
}
