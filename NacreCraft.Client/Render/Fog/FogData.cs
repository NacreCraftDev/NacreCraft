using System.Numerics;

namespace NacreCraft.Client.Render.Fog;

//FogData distances and color the fog shader consumes, maps to vanilla net.minecraft.client.renderer.fog.FogData
public sealed class FogData
{
    public float EnvironmentalStart;
    public float RenderDistanceStart;
    public float EnvironmentalEnd;
    public float RenderDistanceEnd;
    public float SkyEnd;
    public float CloudEnd;
    public Vector4 Color = Vector4.Zero;
}
