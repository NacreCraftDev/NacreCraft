using NacreCraft.Client.Render.State.Gui;
using NacreCraft.Client.Render.State.Level;

namespace NacreCraft.Client.Render.State;

//GameRenderState the whole-frame render state snapshot, maps to vanilla net.minecraft.client.renderer.state.GameRenderState
public sealed class GameRenderState
{
    public readonly LevelRenderState LevelRenderState = new();
    public readonly LightmapRenderState LightmapRenderState = new();
    public readonly GuiRenderState GuiRenderState = new();
    public readonly OptionsRenderState OptionsRenderState = new();
    public readonly WindowRenderState WindowRenderState = new();
    public int FramerateLimit;

    //UseShaderTransparency whether the improved transparency shader path should run
    public bool UseShaderTransparency()
        => !LevelRenderState.CameraRenderState.IsPanoramicMode && OptionsRenderState.ImprovedTransparency;
}
