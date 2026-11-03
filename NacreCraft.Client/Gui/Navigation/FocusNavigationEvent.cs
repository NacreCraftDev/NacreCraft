namespace NacreCraft.Client.Gui.Navigation;

//FocusNavigationEvent describes why focus moved, maps to vanilla FocusNavigationEvent
public interface FocusNavigationEvent
{
    //VerticalDirectionForInitialFocus picks the vertical direction used when no current focus exists
    ScreenDirection GetVerticalDirectionForInitialFocus();

    //ArrowNavigation produced by an arrow key press
    public sealed record ArrowNavigation(ScreenDirection Direction, ScreenRectangle? PreviousFocus = null) : FocusNavigationEvent
    {
        public ScreenDirection GetVerticalDirectionForInitialFocus()
            => Direction.GetAxis() == ScreenAxis.Vertical ? Direction : ScreenDirection.Down;

        public ArrowNavigation WithPreviousFocus(ScreenRectangle previousFocus)
            => this with { PreviousFocus = previousFocus };
    }

    //InitialFocus produced when a screen first selects a focus target
    public sealed class InitialFocus : FocusNavigationEvent
    {
        public ScreenDirection GetVerticalDirectionForInitialFocus() => ScreenDirection.Down;
    }

    //TabNavigation produced by the tab key
    public sealed record TabNavigation(bool Forward) : FocusNavigationEvent
    {
        public ScreenDirection GetVerticalDirectionForInitialFocus()
            => Forward ? ScreenDirection.Down : ScreenDirection.Up;
    }
}
