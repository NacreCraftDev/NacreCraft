namespace NacreCraft.Client.Render.State;

//WindowRenderState window size and GUI scale snapshot, maps to vanilla net.minecraft.client.renderer.state.WindowRenderState
public sealed class WindowRenderState
{
    public int Width;
    public int Height;
    public int GuiScale;
    public float AppropriateLineWidth;
    public bool IsMinimized;
}
