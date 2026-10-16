using System.Numerics;
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

namespace NacreCraft.Client.Blaze3d.Vertex;

//PoseStack matrix stack, maps to vanilla com.mojang.blaze3d.vertex.PoseStack
//The 3D item render chain uses translate/scale to place the item in the atlas slot; item.submit pushPose snapshots the transform
//The normal matrix is the inverse transpose of the pose's 3x3 part; vanilla has a trustedNormals optimization, simplified here to recompute on demand
public sealed class PoseStack
{
    private readonly Stack<Matrix4x4> _poses = new();
    //Normal matrix cache, recomputed from the top pose when dirty
    private Matrix4x4 _normal = Matrix4x4.Identity;
    private bool _normalDirty = true;

    public PoseStack()
    {
        _poses.Push(Matrix4x4.Identity);
    }

    //Pose gets the top model matrix and recomputes the normal matrix on demand
    public Matrix4x4 Pose()
    {
        _normalDirty = true;
        return _poses.Peek();
    }

    //Normal gets the top normal matrix, the inverse transpose 3x3 part of the pose
    public Matrix4x4 Normal()
    {
        if (_normalDirty)
        {
            _normal = ComputeNormalMatrix(_poses.Peek());
            _normalDirty = false;
        }
        return _normal;
    }

    public void PushPose() => _poses.Push(_poses.Peek());
    public void PopPose() => _poses.Pop();
    public bool IsEmpty => _poses.Count == 1;

    //Translate translates the top matrix
    public void Translate(float x, float y, float z)
    {
        var top = _poses.Pop();
        top *= Matrix4x4.CreateTranslation(x, y, z);
        _poses.Push(top);
        _normalDirty = true;
    }

    //Scale scales the top matrix; GuiItemAtlas uses scale(size,-size,size) to flip Y
    public void Scale(float x, float y, float z)
    {
        var top = _poses.Pop();
        top *= Matrix4x4.CreateScale(x, y, z);
        _poses.Push(top);
        _normalDirty = true;
    }

    //MulPose right-multiplies an arbitrary matrix, used to apply itemTransform
    public void MulPose(Matrix4x4 matrix)
    {
        var top = _poses.Pop();
        top *= matrix;
        _poses.Push(top);
        _normalDirty = true;
    }

    //Rotate rotates the top with a quaternion
    public void Rotate(Quaternion rotation)
    {
        var top = _poses.Pop();
        top *= Matrix4x4.CreateFromQuaternion(rotation);
        _poses.Push(top);
        _normalDirty = true;
    }

    public void SetIdentity()
    {
        _poses.Clear();
        _poses.Push(Matrix4x4.Identity);
        _normal = Matrix4x4.Identity;
        _normalDirty = false;
    }

    //TransformPosition transforms a vertex position to the target space with the top pose
    public Vector3 TransformPosition(float x, float y, float z)
        => Vector3.Transform(new Vector3(x, y, z), _poses.Peek());

    //TransformNormal transforms a normal with the normal matrix
    public Vector3 TransformNormal(float x, float y, float z)
        => Vector3.Normalize(Vector3.TransformNormal(new Vector3(x, y, z), Normal()));

    //Copy snapshots the top pose for submit's deferred render, so it can still draw at execute even after poseStack has popped
    public Matrix4x4 Copy() => _poses.Peek();

    //ComputeNormalMatrix computes the normal matrix from the pose matrix, the inverse transpose 3x3 part
    //System.Numerics has no Matrix3x3 so the upper 3x3 of a Matrix4x4 inverse transpose is used
    private static Matrix4x4 ComputeNormalMatrix(Matrix4x4 pose)
    {
        if (Matrix4x4.Invert(pose, out var inv))
            return Matrix4x4.Transpose(inv);
        return Matrix4x4.Identity;
    }
}
