using System.Numerics;
using NacreCraft.Client.Blaze3d;
using NacreCraft.Client.Blaze3d.Systems;

namespace NacreCraft.Client.Render.Rendertype;

//LayeringTransform adjusts the model-view matrix before a render type draws, maps to vanilla net.minecraft.client.renderer.rendertype.LayeringTransform
//System.Numerics matrices are structs, so the transform comes back as a value rather than mutating in place
public sealed class LayeringTransform
{
    public delegate Matrix4x4 MatrixModifier(Matrix4x4 matrix);

    private readonly string _name;
    private readonly MatrixModifier? _modifier;

    public static readonly LayeringTransform NO_LAYERING = new("no_layering", null);
    public static readonly LayeringTransform VIEW_OFFSET_Z_LAYERING = new("view_offset_z_layering", ApplyViewOffsetZ);
    public static readonly LayeringTransform VIEW_OFFSET_Z_LAYERING_FORWARD = new("view_offset_z_layering_forward", ApplyViewOffsetZForward);

    public LayeringTransform(string name, MatrixModifier? modifier)
    {
        _name = name;
        _modifier = modifier;
    }

    public MatrixModifier? GetModifier() => _modifier;

    public override string ToString() => $"LayeringTransform[{_name}]";

    private static Matrix4x4 ApplyViewOffsetZ(Matrix4x4 matrix)
    {
        RenderSystem.GetProjectionType().ApplyLayeringTransform(ref matrix, 1.0f);
        return matrix;
    }

    private static Matrix4x4 ApplyViewOffsetZForward(Matrix4x4 matrix)
    {
        RenderSystem.GetProjectionType().ApplyLayeringTransform(ref matrix, -1.0f);
        return matrix;
    }
}
