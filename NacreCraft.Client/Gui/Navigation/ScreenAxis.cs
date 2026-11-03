namespace NacreCraft.Client.Gui.Navigation;

//ScreenAxis screen axis, maps to vanilla ScreenAxis
public enum ScreenAxis
{
    Horizontal,
    Vertical
}

public static class ScreenAxisExtensions
{
    //Orthogonal returns the perpendicular axis
    public static ScreenAxis Orthogonal(this ScreenAxis axis)
        => axis == ScreenAxis.Horizontal ? ScreenAxis.Vertical : ScreenAxis.Horizontal;

    //GetPositive returns the direction along which coordinates grow
    public static ScreenDirection GetPositive(this ScreenAxis axis)
        => axis == ScreenAxis.Horizontal ? ScreenDirection.Right : ScreenDirection.Down;

    //GetNegative returns the direction along which coordinates shrink
    public static ScreenDirection GetNegative(this ScreenAxis axis)
        => axis == ScreenAxis.Horizontal ? ScreenDirection.Left : ScreenDirection.Up;

    public static ScreenDirection GetDirection(this ScreenAxis axis, bool positive)
        => positive ? axis.GetPositive() : axis.GetNegative();
}
