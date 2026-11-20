using System.Numerics;
using NacreCraft.Client.Render.Fog;
using NacreCraft.Primitives;

namespace NacreCraft.Client.Render.State.Level;

//CameraRenderState the camera snapshot shared by the level renderers, maps to vanilla
//net.minecraft.client.renderer.state.level.CameraRenderState
//NC keeps only the fields its renderer consumes; frustum/fog fields land when those systems are ported
public sealed class CameraRenderState
{
    public BlockPos BlockPos = BlockPos.Zero;
    public Vec3 Pos = Vec3.Zero;
    public float XRot;
    public float YRot;
    public bool Initialized;
    public bool IsPanoramicMode;
    public bool IsFrustumCaptured;
    public bool SmartCull;
    public Quaternion Orientation = Quaternion.Identity;
    public float HudFov;
    public float DepthFar;
    public Matrix4x4 ProjectionMatrix = Matrix4x4.Identity;
    public Matrix4x4 ViewRotationMatrix = Matrix4x4.Identity;
    public FogData FogData = new();
}
