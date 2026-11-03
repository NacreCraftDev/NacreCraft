using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
using NacreCraft.Client.Render;
using NacreCraft.Client.Render.Model;
using NacreCraft.Client.Render.Texture;
using NacreCraft.Client.Render.Texture.Atlas;
using NacreCraft.Client.Render.Item;
using NacreCraft.Client.Render.Entity;
using NacreCraft.Client.Render.Entity.State;
using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Gui;
using NacreCraft.Client.Gui.Render;
using NacreCraft.Client.Gui.Render.State;
using NacreCraft.Client.Gui.Render.Pip;
using NacreCraft.Client.Gui.Navigation;
using NacreCraft.Client.Gui.Layouts;
using NacreCraft.Client.Gui.Font;
using NacreCraft.Client.Gui.Font.Providers;
using NacreCraft.Client.Gui.Font.Glyphs;
using NacreCraft.Client.Model;
using NacreCraft.Client.Model.Geom;
using NacreCraft.Client.Resources.Metadata.Gui;
using NacreCraft.Client.Blaze3d.Platform.Cursor;
using NacreCraft.Logging;
namespace NacreCraft.Client.Gui;

//GuiWindow top-level GUI window
//Hosts the widget tree and exposes event input and the Render interface
//Does not manage Vulkan resources directly; an external caller passes in an IGuiRenderContext for rendering
public sealed class GuiWindow : GuiContainer
{
    private GuiControl? _hoveredControl;
    //_shiftDown/_ctrlDown maintained by ProcessKeyDown/Up from the Shift/Ctrl key codes
    //TextBox checks e.Modifiers to extend the selection or handle Ctrl+C/V/X combos
    private bool _shiftDown;
    private bool _ctrlDown;

    //SurfaceWidth/Height swapchain actual pixels, used for scissor/pipeline extent
    //ScaledWidth/Height logical pixels floor(Surface/GuiScale), used for widget layout
    //GuiScale integer factor max(1, min(SurfaceW/320, SurfaceH/240)) scaling widgets uniformly without blur
    public int SurfaceWidth { get; set; }
    public int SurfaceHeight { get; set; }
    public int GuiScale { get; private set; } = 1;
    public int ScaledWidth { get; private set; }
    public int ScaledHeight { get; private set; }

    private GuiColor _backgroundColor = GuiColor.FromRgb(30, 30, 30);
    //BackgroundColor background color; the setter MarkDirty's so a change re-Renders the whole window and records a new background quad
    public GuiColor BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; MarkDirty(); }
    }

    //RenderBackgroundHook screen-level background callback injected by ScreenManager.SetScreen
    //Screen is in the Game layer and Window in the GPU layer; they cannot call each other directly, so this hook bridges them
    //Called on Render after the background quad and before widgets so a dirt background covers the solid color
    private Action<IGuiRenderContext>? _renderBackgroundHook;
    public Action<IGuiRenderContext>? RenderBackgroundHook
    {
        get => _renderBackgroundHook;
        set { _renderBackgroundHook = value; MarkDirty(); }
    }

    //RenderForegroundHook screen-level foreground callback injected by ScreenManager.SetScreen
    //Called on Render after widgets, not cached; HUD hearts animate every frame and skip the retained-mode cache
    public Action<IGuiRenderContext>? RenderForegroundHook { get; set; }

    //WantsBlur whether the current Screen needs blur post-processing, injected by ScreenManager.SetScreen from Screen.WantsBlur
    //Blur frames skip the retained-mode cache; BlurBeforeThisStratum is not recorded and must be called every frame
    public bool WantsBlur { get; set; }
    //s_renderDiag throttles the first frames' render stage tracing
    private static int s_renderDiag;

    //FocusedControl the widget currently receiving keyboard input
    //SetFocused is the normal writer so both widgets get notified; the internal setter only serves tests
    public GuiControl? FocusedControl { get; internal set; }

    //CursorApplier applies a cursor shape to the platform window, injected by VulkanGuiApp
    public Action<CursorType>? CursorApplier { get; set; }
    //_appliedCursor the shape last sent out; cursor types are static instances so a reference check is enough
    private CursorType? _appliedCursor;

    public GuiWindow(int surfaceWidth, int surfaceHeight)
    {
        UpdateSurfaceSize(surfaceWidth, surfaceHeight);
    }

    //UpdateSurfaceSize called by VulkanGuiApp on creation and swapchain recreation
    //Computes GuiScale from actual pixels and updates the Scaled sizes; its own Width/Height use scaled for layout
    //MinScaled 320x240 corresponds to the vanilla GUI minimum readable size; guiScale will not shrink widgets below this
    public void UpdateSurfaceSize(int actualW, int actualH)
    {
        const int MinScaledWidth = 320;
        const int MinScaledHeight = 240;
        var maxScaleW = actualW / MinScaledWidth;
        var maxScaleH = actualH / MinScaledHeight;
        var scale = Math.Max(1, Math.Min(maxScaleW, maxScaleH));
        GuiScale = scale;
        ScaledWidth = actualW / scale;
        ScaledHeight = actualH / scale;
        SurfaceWidth = actualW;
        SurfaceHeight = actualH;
        X = 0;
        Y = 0;
        Width = ScaledWidth;
        Height = ScaledHeight;
    }

    //ProcessMouseDown handles the mouse-down event and dispatches it to the hit widget automatically
    //The Silk callback gives actual pixels; divide by GuiScale to scaled, then HitTest to match widget layout coordinates
    //On focus change it calls OnLostFocus on the old widget and OnGotFocus on the new, letting TextBox start/stop caret blinking
    public void ProcessMouseDown(GuiMouseButton button, int x, int y)
    {
        x /= GuiScale;
        y /= GuiScale;
        var hit = HitTest(x, y);
        if (hit is null)
        {
            //A click outside every widget clears focus, as vanilla does
            SetFocused(null);
            return;
        }
        SetFocused(hit);
        var args = new MouseEventArgs(button, x, y, CurrentModifiers());
        if (hit == this)
        {
            base.OnMouseDown(args);
        }
        else
        {
            hit.OnMouseDown(args);
        }
        //A press can start a drag, which changes the cursor the widget asks for
        ApplyCursor();
    }

    //ApplyCursor asks the platform window for the hovered widget's cursor, only when the shape changes
    private void ApplyCursor()
    {
        var cursor = _hoveredControl?.RequestedCursor ?? CursorTypes.Arrow;
        if (ReferenceEquals(cursor, _appliedCursor)) return;
        _appliedCursor = cursor;
        CursorApplier?.Invoke(cursor);
    }

    //SetFocused moves keyboard focus and notifies both widgets, maps to vanilla setFocused
    public void SetFocused(GuiControl? next)
    {
        var previous = FocusedControl;
        if (ReferenceEquals(previous, next)) return;
        if (previous is not null)
        {
            previous.IsFocused = false;
            previous.OnLostFocus();
        }
        FocusedControl = next;
        if (next is not null)
        {
            next.IsFocused = true;
            next.OnGotFocus();
        }
    }

    //ProcessMouseUp handles mouse-up
    //Click firing is up to the widget; GuiButton calls OnMouseClick in OnMouseUp after checking the press position matches
    public void ProcessMouseUp(GuiMouseButton button, int x, int y)
    {
        x /= GuiScale;
        y /= GuiScale;
        var hit = HitTest(x, y);
        if (hit is null) return;
        var args = new MouseEventArgs(button, x, y, CurrentModifiers());
        if (hit == this)
        {
            base.OnMouseUp(args);
        }
        else
        {
            hit.OnMouseUp(args);
        }
        //Releasing ends a drag, which changes the cursor the widget asks for
        ApplyCursor();
    }

    //ProcessMouseMove handles mouse movement and maintains hover state, firing enter/leave events
    public void ProcessMouseMove(int x, int y)
    {
        x /= GuiScale;
        y /= GuiScale;
        var args = new MouseEventArgs(GuiMouseButton.None, x, y, CurrentModifiers());
        var hit = HitTest(x, y);

        if (!ReferenceEquals(hit, _hoveredControl))
        {
            _hoveredControl?.OnMouseLeave(args);
            _hoveredControl = hit;
            hit?.OnMouseEnter(args);
            //Hover changes the drawing of two controls, and the scene is only rebuilt when something marks it dirty,
            //so without this the highlight never follows the cursor
            MarkDirty();
            ApplyCursor();
        }

        if (hit is null)
        {
            base.OnMouseMove(args);
            return;
        }
        if (hit == this)
        {
            base.OnMouseMove(args);
        }
        else
        {
            hit.OnMouseMove(args);
        }
    }

    //GLFW_KEY_TAB key code; Tab triggers focus navigation and is not dispatched to the focused widget
    private const int TabKey = 258;

    //ArrowDirections maps arrow key codes to the navigation direction they request
    private static readonly Dictionary<int, ScreenDirection> ArrowDirections = new()
    {
        [GuiKeys.Up] = ScreenDirection.Up,
        [GuiKeys.Down] = ScreenDirection.Down,
        [GuiKeys.Left] = ScreenDirection.Left,
        [GuiKeys.Right] = ScreenDirection.Right
    };

    //ProcessKeyDown handles key-down and dispatches to the focused widget
    //Tab triggers FocusNext focus navigation; with character input it calls OnKeyPress for TextBox text accumulation
    //First UpdateModifiers maintains the Shift/Ctrl state, builds KeyEventArgs and passes Modifiers for TextBox to detect combos
    public void ProcessKeyDown(int key, char ch = '\0')
    {
        UpdateModifiers(key, true);
        if (key == TabKey)
        {
            //Shift reverses the walk, maps to vanilla TabNavigation.forward
            FocusNext(forward: !_shiftDown);
            return;
        }
        //Arrow keys navigate focus geometrically unless the focused widget edits with that key, maps to vanilla arrow navigation
        if (ArrowDirections.TryGetValue(key, out var direction)
            && (FocusedControl is null || FocusedControl == this || !FocusedControl.ConsumesArrowKey(key)))
        {
            FocusDirection(direction);
            return;
        }
        var args = new KeyEventArgs(key, ch, CurrentModifiers());
        if (FocusedControl is not null && FocusedControl != this)
        {
            FocusedControl.OnKeyDown(args);
            if (ch != '\0')
            {
                FocusedControl.OnKeyPress(args);
            }
        }
        else
        {
            base.OnKeyDown(args);
            if (ch != '\0') base.OnKeyPress(args);
        }
    }

    //UpdateModifiers updates the _shiftDown/_ctrlDown state from key
    //Shift/Ctrl set true on press and false on release; Alt is not tracked as TextBox does not need it
    private void UpdateModifiers(int key, bool down)
    {
        if (key == GuiKeys.LeftShift || key == GuiKeys.RightShift) _shiftDown = down;
        else if (key == GuiKeys.LeftControl || key == GuiKeys.RightControl) _ctrlDown = down;
    }

    //CurrentModifiers combines _shiftDown/_ctrlDown into KeyModifiers flags
    private KeyModifiers CurrentModifiers()
    {
        var m = KeyModifiers.None;
        if (_shiftDown) m |= KeyModifiers.Shift;
        if (_ctrlDown) m |= KeyModifiers.Control;
        return m;
    }

    //ProcessKeyUp handles key-up
    public void ProcessKeyUp(int key, char ch = '\0')
    {
        UpdateModifiers(key, false);
        var args = new KeyEventArgs(key, ch, CurrentModifiers());
        if (FocusedControl is not null && FocusedControl != this)
        {
            FocusedControl.OnKeyUp(args);
        }
        else
        {
            base.OnKeyUp(args);
        }
    }

    //ProcessKeyChar handles character input, dispatching OnKeyPress only to the focused widget for TextBox accumulation
    //Short-circuits when _ctrlDown so Ctrl combos do not insert text, avoiding Ctrl+V pasting while also inserting 'v'
    public void ProcessKeyChar(char ch)
    {
        if (_ctrlDown) return;
        var args = new KeyEventArgs(0, ch, CurrentModifiers());
        if (FocusedControl is not null && FocusedControl != this)
            FocusedControl.OnKeyPress(args);
        else
            base.OnKeyPress(args);
    }

    //FocusNext moves focus to the next TabStop widget by TabIndex order, wrapping at both ends
    //forward=false is Shift+Tab, matching vanilla TabNavigation.forward
    //Before switching it calls OnLostFocus on the old widget and OnGotFocus on the new, letting TextBox start/stop caret blinking
    public void FocusNext(bool forward = true)
    {
        var tabStops = new List<GuiControl>();
        CollectTabStops(this, tabStops);
        if (tabStops.Count == 0) return;
        //OrderBy is stable, so widgets sharing a TabIndex keep their tree order
        tabStops = tabStops.OrderBy(control => control.TabIndex).ToList();
        var currentIdx = FocusedControl is null ? -1 : tabStops.IndexOf(FocusedControl);
        //A missing current index starts at the first widget forward, the last widget backward
        var nextIdx = forward
            ? (currentIdx + 1) % tabStops.Count
            : (currentIdx <= 0 ? tabStops.Count : currentIdx) - 1;
        SetFocused(tabStops[nextIdx]);
    }

    //CollectTabStops depth-walks the widget tree collecting widgets with TabStop=true, Visible and Enabled
    private static void CollectTabStops(GuiContainer container, List<GuiControl> result)
    {
        foreach (var child in container.Children)
        {
            if (child is GuiContainer nested)
                CollectTabStops(nested, result);
            if (child.TabStop && child.Visible && child.Enabled)
                result.Add(child);
        }
    }

    //FocusDirection moves focus to the nearest widget along the direction, maps to vanilla arrow navigation
    public void FocusDirection(ScreenDirection direction)
    {
        var candidates = new List<GuiControl>();
        CollectTabStops(this, candidates);
        if (candidates.Count == 0) return;

        //Without a current focus the search starts from the window's opposite border, as vanilla does
        var origin = FocusedControl is { } focused
            ? ToScreenRectangle(focused).GetBorder(direction)
            : ToScreenRectangle(this).GetBorder(direction.GetOpposite());
        var next = FindFocusInDirection(origin, direction, FocusedControl, candidates)
            ?? FindFocusVaguelyInDirection(origin, direction, FocusedControl, candidates);
        if (next is null) return;
        SetFocused(next);
    }

    //FindFocusInDirection prefers candidates overlapping the orthogonal axis, maps to vanilla nextFocusPathInDirection
    private static GuiControl? FindFocusInDirection(ScreenRectangle origin, ScreenDirection direction, GuiControl? excluded, List<GuiControl> candidates)
    {
        var otherAxis = direction.GetAxis().Orthogonal();
        var positiveOther = otherAxis.GetPositive();
        var originFirstBound = origin.GetBoundInDirection(direction.GetOpposite());
        var potential = new List<GuiControl>();
        foreach (var candidate in candidates)
        {
            if (candidate == excluded) continue;
            var rectangle = ToScreenRectangle(candidate);
            if (!rectangle.OverlapsInAxis(origin, otherAxis)) continue;
            var candidateFirstBound = rectangle.GetBoundInDirection(direction.GetOpposite());
            if (direction.IsAfter(candidateFirstBound, originFirstBound))
            {
                potential.Add(candidate);
                continue;
            }
            if (candidateFirstBound != originFirstBound) continue;
            if (!direction.IsAfter(rectangle.GetBoundInDirection(direction), origin.GetBoundInDirection(direction))) continue;
            potential.Add(candidate);
        }
        var comparePrimary = direction.CoordinateValueComparator();
        var compareSecondary = positiveOther.CoordinateValueComparator();
        potential.Sort((a, b) =>
        {
            var rectangleA = ToScreenRectangle(a);
            var rectangleB = ToScreenRectangle(b);
            var primary = comparePrimary(
                rectangleA.GetBoundInDirection(direction.GetOpposite()),
                rectangleB.GetBoundInDirection(direction.GetOpposite()));
            if (primary != 0) return primary;
            return compareSecondary(
                rectangleA.GetBoundInDirection(positiveOther.GetOpposite()),
                rectangleB.GetBoundInDirection(positiveOther.GetOpposite()));
        });
        return potential.Count > 0 ? potential[0] : null;
    }

    //FindFocusVaguelyInDirection falls back to the nearest center distance along the direction, maps to vanilla nextFocusPathVaguelyInDirection
    private static GuiControl? FindFocusVaguelyInDirection(ScreenRectangle origin, ScreenDirection direction, GuiControl? excluded, List<GuiControl> candidates)
    {
        var axis = direction.GetAxis();
        var otherAxis = axis.Orthogonal();
        var originSideCenter = ScreenPosition.Of(axis, origin.GetBoundInDirection(direction), origin.GetCenterInAxis(otherAxis));
        var potential = new List<(GuiControl Control, long Distance)>();
        foreach (var candidate in candidates)
        {
            if (candidate == excluded) continue;
            var rectangle = ToScreenRectangle(candidate);
            var candidateSideCenter = ScreenPosition.Of(axis, rectangle.GetBoundInDirection(direction.GetOpposite()), rectangle.GetCenterInAxis(otherAxis));
            if (!direction.IsAfter(candidateSideCenter.GetCoordinate(axis), originSideCenter.GetCoordinate(axis))) continue;
            var dx = (long)originSideCenter.X - candidateSideCenter.X;
            var dy = (long)originSideCenter.Y - candidateSideCenter.Y;
            potential.Add((candidate, dx * dx + dy * dy));
        }
        if (potential.Count == 0) return null;
        potential.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return potential[0].Control;
    }

    private static ScreenRectangle ToScreenRectangle(GuiControl control)
        => new(control.X, control.Y, control.Width, control.Height);

    public override void Render(IGuiRenderContext context)
    {
        //Top-level window cache: when the whole window is dirty it re-Renders and records, otherwise ReplayRange replays and skips Render
        //Blur frames force a re-Render; BlurBeforeThisStratum is not recorded, so replay would lose the blur marker
        var trace = s_renderDiag < 4;
        s_renderDiag++;
        if (trace) Log.Debug($"gui window render enter dirty={_isDirty} cache={_renderCache is not null} blur={WantsBlur}");
        if (!_isDirty && _renderCache is not null && !WantsBlur)
        {
            context.ReplayRange(_renderCache);
            if (trace) Log.Debug("gui window render replayed");
        }
        else
        {
            _renderCache ??= new();
            _renderCache.Clear();
            context.BeginRecording(_renderCache);
            //The background uses ScaledWidth/Height logical pixels; the ToClip conversion uses scaled as the denominator for full coverage
            context.DrawQuad(0, 0, ScaledWidth, ScaledHeight, BackgroundColor);
            if (trace) Log.Debug("gui window render background done");
            //RenderBackgroundHook draws the screen-level background texture over the solid color
            RenderBackgroundHook?.Invoke(context);
            if (trace) Log.Debug("gui window render screen background done");
            base.Render(context);
            if (trace) Log.Debug("gui window render widgets done");
            context.EndRecording();
            ClearDirtyTree();
        }
        //RenderForegroundHook draws directly every frame after widgets without caching; HUD hearts change every frame
        RenderForegroundHook?.Invoke(context);
        if (trace) Log.Debug("gui window render exit");
    }
}
