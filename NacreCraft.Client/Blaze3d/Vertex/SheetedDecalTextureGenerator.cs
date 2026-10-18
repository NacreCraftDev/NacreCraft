using System.Numerics;
using Direction = NacreCraft.Primitives.Direction;

namespace NacreCraft.Client.Blaze3d.Vertex;

//SheetedDecalTextureGenerator retextures geometry so that it reads as painted onto the face of a block
//maps to vanilla com.mojang.blaze3d.vertex.SheetedDecalTextureGenerator
//It wraps another consumer and derives the UVs itself: the vertex position goes through the inverse camera pose and then
//the face rotation, and the resulting plane coordinates scale into texture space. An arrow stuck into a block is the
//classic user, which is why the colour is forced to white and the incoming UVs are dropped
public sealed class SheetedDecalTextureGenerator : VertexConsumer
{
    private readonly VertexConsumer _target;
    private readonly Matrix4x4 _cameraInversePose;
    private readonly Matrix4x4 _normalInversePose;
    private readonly float _textureScale;
    private float _x;
    private float _y;
    private float _z;

    public SheetedDecalTextureGenerator(VertexConsumer target, PoseStack.Pose cameraPose, float textureScale)
    {
        _target = target;
        _textureScale = textureScale;
        //Vanilla inverts both poses up front so the per vertex cost is one transform plus one quaternion rotation
        _cameraInversePose = Matrix4x4.Invert(cameraPose.Model, out var inverseCamera) ? inverseCamera : Matrix4x4.Identity;
        _normalInversePose = Matrix4x4.Invert(cameraPose.Normal, out var inverseNormal) ? inverseNormal : Matrix4x4.Identity;
    }

    //AddVertex remembers the position, SetNormal needs it to derive the decal UVs
    public void AddVertex(float x, float y, float z)
    {
        _x = x;
        _y = y;
        _z = z;
        _target.AddVertex(x, y, z);
    }

    //SetColor a decal keeps the texture's own colours, so every colour request collapses to white
    public void SetColor(int red, int green, int blue, int alpha) => _target.SetColor(-1);

    //SetUv is dropped: deriving the UVs is what this class exists for
    public void SetUv(float u, float v)
    {
    }

    public void SetUv1(int u, int v) => _target.SetUv1(u, v);

    public void SetUv2(int u, int v) => _target.SetUv2(u, v);

    //SetNormal computes the decal UVs from the position and the face the normal points at, maps to vanilla setNormal
    public void SetNormal(float x, float y, float z)
    {
        _target.SetNormal(x, y, z);
        var normal = Vector3.TransformNormal(new Vector3(x, y, z), _normalInversePose);
        var direction = Direction.FromViewVector(normal.X, normal.Y, normal.Z);
        var worldPosition = Vector3.Transform(new Vector3(_x, _y, _z), _cameraInversePose);
        //Lay the plane down to face up, then turn it onto the face the normal points at
        worldPosition = Vector3.Transform(worldPosition, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI));
        worldPosition = Vector3.Transform(worldPosition, Quaternion.CreateFromAxisAngle(Vector3.UnitX, -1.5707964f));
        worldPosition = Vector3.Transform(worldPosition, direction.GetRotation());
        _target.SetUv(-worldPosition.X * _textureScale, -worldPosition.Y * _textureScale);
    }

    public void SetLineWidth(float width) => _target.SetLineWidth(width);
}
