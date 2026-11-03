namespace NacreCraft.Client.Gui.Navigation;

//ScreenDirection screen direction, maps to vanilla ScreenDirection
//Declaration order matters: Vertical directions come first, matching vanilla ordinals
public enum ScreenDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class ScreenDirectionExtensions
{
    //GetAxis returns the axis the direction moves along
    public static ScreenAxis GetAxis(this ScreenDirection direction)
        => direction is ScreenDirection.Up or ScreenDirection.Down ? ScreenAxis.Vertical : ScreenAxis.Horizontal;

    public static ScreenDirection GetOpposite(this ScreenDirection direction)
        => direction switch
        {
            ScreenDirection.Up => ScreenDirection.Down,
            ScreenDirection.Down => ScreenDirection.Up,
            ScreenDirection.Left => ScreenDirection.Right,
            _ => ScreenDirection.Left
        };

    //IsPositive whether the direction increases the coordinate value
    public static bool IsPositive(this ScreenDirection direction)
        => direction is ScreenDirection.Down or ScreenDirection.Right;

    //IsAfter whether a is farther along the direction than b
    public static bool IsAfter(this ScreenDirection direction, int a, int b)
        => direction.IsPositive() ? a > b : b > a;

    //IsBefore whether a is earlier along the direction than b
    public static bool IsBefore(this ScreenDirection direction, int a, int b)
        => direction.IsPositive() ? a < b : b < a;

    //CoordinateValueComparator orders coordinates by sweeping along the direction
    public static Comparison<int> CoordinateValueComparator(this ScreenDirection direction)
        => (a, b) => a == b ? 0 : (direction.IsBefore(a, b) ? -1 : 1);
}
