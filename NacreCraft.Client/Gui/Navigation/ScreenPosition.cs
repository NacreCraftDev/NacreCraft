namespace NacreCraft.Client.Gui.Navigation;

//ScreenPosition integer screen coordinate, maps to vanilla ScreenPosition
public readonly record struct ScreenPosition(int X, int Y)
{
    //Of builds a position from an axis and its primary/secondary values
    public static ScreenPosition Of(ScreenAxis axis, int primaryValue, int secondaryValue)
        => axis == ScreenAxis.Horizontal
            ? new ScreenPosition(primaryValue, secondaryValue)
            : new ScreenPosition(secondaryValue, primaryValue);

    public ScreenPosition Step(ScreenDirection direction)
        => direction switch
        {
            ScreenDirection.Down => new ScreenPosition(X, Y + 1),
            ScreenDirection.Up => new ScreenPosition(X, Y - 1),
            ScreenDirection.Left => new ScreenPosition(X - 1, Y),
            _ => new ScreenPosition(X + 1, Y)
        };

    public int GetCoordinate(ScreenAxis axis)
        => axis == ScreenAxis.Horizontal ? X : Y;
}
