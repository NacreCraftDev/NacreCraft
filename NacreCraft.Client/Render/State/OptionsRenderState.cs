using NacreCraft.Client;

namespace NacreCraft.Client.Render.State;

//OptionsRenderState the option values the renderer reads for one frame, maps to vanilla net.minecraft.client.renderer.state.OptionsRenderState
public sealed class OptionsRenderState
{
    public int CloudRange;
    public bool CutoutLeaves;
    public bool ImprovedTransparency;
    public bool AmbientOcclusion;
    public int MenuBackgroundBlurriness;
    public double PanoramaSpeed;
    public int MaxAnisotropyValue;
    public TextureFilteringMethod TextureFiltering = TextureFilteringMethod.None;
    public bool BobView;
    public float ScreenEffectScale;
    public double GlintSpeed = 0.5;
    public double GlintStrength;
    public double DamageTiltStrength;
    public bool BackgroundForChatOnly;
    public float TextBackgroundOpacity;
    public CloudStatus CloudStatus = CloudStatus.Off;
    public CameraType CameraType = CameraType.FirstPerson;
    public int RenderDistance;
    public double ChunkSectionFadeInTime;
    public PrioritizeChunkUpdates PrioritizeChunkUpdates;
    public int Fov;

    //GetBackgroundOpacity chat background opacity, maps to vanilla getBackgroundOpacity
    public float GetBackgroundOpacity(float defaultOpacity)
        => BackgroundForChatOnly ? defaultOpacity : TextBackgroundOpacity;
}
