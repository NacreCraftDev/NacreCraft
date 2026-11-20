using NacreCraft.Client.Render.Entity.State;

namespace NacreCraft.Client.Render.State.Level;

//LevelRenderState the per-frame level render snapshot, maps to vanilla net.minecraft.client.renderer.state.level.LevelRenderState
//NC keeps the camera and entity slices its renderer consumes; the remaining sub-states are added with their renderers
public sealed class LevelRenderState
{
    public CameraRenderState CameraRenderState = new();
    public readonly List<EntityRenderState> EntityRenderStates = new();
    public long GameTime;
    public int LastEntityRenderStateCount;
    public int CloudColor;
    public float CloudHeight;
    public bool Render3dCrosshair;
    public bool ShouldResetChunkLayerSampler;
    public bool ShouldShowEntityOutlines;
    public bool ShouldResetSkyRenderer;

    public void Reset()
    {
        EntityRenderStates.Clear();
        GameTime = 0L;
        LastEntityRenderStateCount = 0;
        ShouldShowEntityOutlines = false;
    }
}
