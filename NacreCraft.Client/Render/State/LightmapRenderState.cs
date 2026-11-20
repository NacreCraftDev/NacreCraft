using System.Numerics;

namespace NacreCraft.Client.Render.State;

//LightmapRenderState lightmap inputs for the current frame, maps to vanilla net.minecraft.client.renderer.state.LightmapRenderState
public sealed class LightmapRenderState
{
    public bool NeedsUpdate;
    public float BlockFactor;
    public Vector3 BlockLightTint = Vector3.One;
    public float SkyFactor;
    public Vector3 SkyLightColor = Vector3.One;
    public Vector3 AmbientColor = Vector3.One;
    public float Brightness;
    public float DarknessEffectScale;
    public float NightVisionEffectIntensity;
    public Vector3 NightVisionColor = Vector3.One;
    public float BossOverlayWorldDarkening;
}
