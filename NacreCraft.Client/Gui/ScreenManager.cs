using NacreCraft.Client;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Logging;
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

namespace NacreCraft.Client.Gui;

//ScreenManager screen manager, maps to vanilla Minecraft.screen field
//Holds the current Screen and drives its lifecycle; on switch it clears old controls, calls the old Removed and the new Init
//A history stack supports PushScreen/PopScreen back to the previous level; the Esc key triggers the current Screen.OnClose
public sealed class ScreenManager
{
    private readonly MinecraftClient _minecraft;
    private readonly GuiWindow _window;
    private Screen? _current;
    //_history screen history stack; PushScreen pushes, PopScreen pops back to the previous level
    private readonly Stack<Screen> _history = new();
    //_layoutDirty window resize flag set by SwapchainRecreated and consumed by ProcessLayoutIfDirty on the Tick thread
    //In phase 7 the control tree belongs to the Tick thread; the resize callback cannot call Window.Clear/Screen.Init directly and must defer to Tick
    private volatile bool _layoutDirty;
    //_overlay layer drawn above the current screen; it ticks with the screen and is dropped once it reports finished
    private IGuiOverlay? _overlay;

    public Screen? Current => _current;

    //Overlay the overlay currently installed, null when there is none; while one is up the screen gets no input
    public IGuiOverlay? Overlay => _overlay;

    public ScreenManager(MinecraftClient minecraft, GuiWindow window)
    {
        _minecraft = minecraft;
        _window = window;
    }

    //SetScreen switches screens: Removed on the old one, clear controls, then Init the new one; does not enter the history stack
    //Also injects RenderBackgroundHook so Screen.RenderBackground draws in the Window's background layer
    public void SetScreen(Screen? screen)
    {
        var old = _current;
        if (old is not null)
        {
            old.Removed();
            Log.Debug($"SetScreen exit old={old.Title}");
        }
        _window.Clear();
        _current = screen;
        //When screen is null, clear the background/foreground hooks to avoid render residue from the old Screen
        _window.RenderBackgroundHook = screen is null ? null : ctx => screen.RenderBackground(ctx);
        //The foreground hook always goes through the chain so an overlay still renders when the screen is null
        _window.RenderForegroundHook = RenderForegroundChain;
        //WantsBlur is injected from the Screen into GuiWindow, triggering segmented rendering in RenderBlurPasses
        _window.WantsBlur = screen?.WantsBlur ?? false;
        if (screen is null) return;
        Log.Debug($"SetScreen entry new={screen.Title}");
        screen.Attach(_minecraft, _window, this);
        screen.Init();
    }

    //PushScreen pushes the current screen onto the stack and switches to a new screen, for entering submenus
    public void PushScreen(Screen screen)
    {
        if (_current is not null) _history.Push(_current);
        SetScreen(screen);
    }

    //PopScreen pops back to the previous level; if the stack is empty, falls back to null. Triggered by Esc or OnClose
    public void PopScreen()
    {
        if (_history.Count > 0) SetScreen(_history.Pop());
        else SetScreen(null);
    }

    //HandleRawKeyDown handles raw key codes, recognizing business keys and calling the current screen's matching callback
    //Esc switches screens, F3 toggles Debug, number keys select Hotbar slots; other business keys (Q drop, E inventory) are passed through as-is and not consumed
    public void HandleRawKeyDown(int key)
    {
        //An overlay owns the input while it is up, matching vanilla where the screen does not receive it
        if (_overlay is not null) return;
        if (_current is null) return;
        if (key == GameKeys.Escape)
            _current.OnClose();
        else if (key == GameKeys.F3)
            _current.OnF3Pressed();
        else if (key >= GameKeys.D1 && key <= GameKeys.D9)
            _current.OnHotbarSelect(key - GameKeys.D1);
        else if (key >= GameKeys.Keypad1 && key <= GameKeys.Keypad9)
            _current.OnHotbarSelect(key - GameKeys.Keypad1);
        else
            _current.OnKeyPressed(key);
    }

    //HandleRawMouseDown forwards raw mouse down to the current screen; world interaction (block breaking) goes this way
    public void HandleRawMouseDown(GuiMouseButton button, int x, int y)
    {
        if (_overlay is not null) return;
        _current?.OnMouseDown(button, x, y);
    }

    //HandleRawMouseUp forwards raw mouse up to the current screen
    public void HandleRawMouseUp(GuiMouseButton button, int x, int y)
    {
        if (_overlay is not null) return;
        _current?.OnMouseUp(button, x, y);
    }

    //MouseX/MouseY most recent mouse position, converted to control coordinates by GuiScale
    //The inventory screen uses it to determine the hovered slot; Q drop acts on the hovered slot
    public int MouseX { get; private set; }
    public int MouseY { get; private set; }

    //HandleRawMouseMove records the mouse position, converting window coordinates to control coordinates
    public void HandleRawMouseMove(int x, int y)
    {
        var scale = Math.Max(1, _window.GuiScale);
        MouseX = x / scale;
        MouseY = y / scale;
    }

    public void Tick()
    {
        _current?.Tick();
        if (_overlay is null) return;
        _overlay.Tick();
        if (_overlay.IsFinished) SetOverlay(null);
    }

    //SetOverlay installs the layer drawn above the current screen; null removes it
    //Vanilla drives the loading overlay this way: it ticks with the screen and is dropped once it reports finished
    public void SetOverlay(IGuiOverlay? overlay)
    {
        _overlay = overlay;
    }

    //RebuildCurrent re-runs the current screen's Init, maps to vanilla reinitializing the screen when a reload finishes
    //The loading overlay calls it once its work is done, so text taken from the language table is refreshed
    public void RebuildCurrent()
    {
        if (_current is null) return;
        _window.Clear();
        _current.Init();
    }

    //RenderForegroundChain draws the screen foreground first and the overlay after it, so an overlay always stays on top
    private void RenderForegroundChain(IGuiRenderContext context)
    {
        _current?.RenderForeground(context);
        _overlay?.Render(context);
    }

    //Resized triggered by MinecraftClient subscribing to SwapchainRecreated when the window size changes
    //In phase 7 the control tree belongs to the Tick thread; the resize callback fires on the Render thread and cannot call Window.Clear/Screen.Init directly
    //Only sets the _layoutDirty flag; ProcessLayoutIfDirty consumes it on the Tick thread from MinecraftClient.OnFrameTick
    public void Resized()
    {
        _layoutDirty = true;
    }

    //ProcessLayoutIfDirty called every frame on the Tick thread; when _layoutDirty is true it clears controls and re-Inits the current Screen
    //Called by MinecraftClient.OnFrameTick after PollInput and before Window.Update; it holds the control tree exclusively, with no contention
    public void ProcessLayoutIfDirty()
    {
        if (!_layoutDirty) return;
        _layoutDirty = false;
        if (_current is null) return;
        _window.Clear();
        _current.Init();
    }
}
