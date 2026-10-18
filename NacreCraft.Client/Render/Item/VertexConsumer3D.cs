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

namespace NacreCraft.Client.Render.Item;

//VertexConsumer3D 3D vertex consumer, maps to vanilla VertexConsumer
//Implements IVertexConsumer, writing 3D vertices into List<float> for the P14 chain ItemPipRenderer/ItemItemAtlas to read bytes and upload to the GPU
//The PutBakedQuad static method accepts any IVertexConsumer, writing either to VertexConsumer3D itself or to StagedVertexBuffer.VertexBuilder
//Vertex format position(3f)+color(1f)+uv(2f)+light(1f)+normal(3f), a simplification of vanilla without overlay
//color/light are ints written as converted floats and restored with int() in the shader; 0xFFFFFFFF's bit pattern is NaN so a bit-pattern conversion cannot be used
public sealed class VertexConsumer3D : VertexConsumer
{
    //Vertices written vertex data, 10 floats per vertex position3+color1+uv2+light1+normal3
    //The P14 chain uses MemoryMarshal.AsBytes to memcpy directly for GPU upload
    public readonly List<float> Vertices = new();

    private int _color = -1;
    private float _u;
    private float _v;
    private int _light;
    private float _nx;
    private float _ny;
    private float _nz;

    //AddVertex finishes a vertex, writing the accumulated attributes into the fixed 10 float layout
    public void AddVertex(float x, float y, float z)
    {
        Vertices.Add(x); Vertices.Add(y); Vertices.Add(z);
        Vertices.Add((float)_color);
        Vertices.Add(_u); Vertices.Add(_v);
        Vertices.Add((float)_light);
        Vertices.Add(_nx); Vertices.Add(_ny); Vertices.Add(_nz);
    }

    public void SetColor(int red, int green, int blue, int alpha)
        => _color = (alpha << 24) | (red << 16) | (green << 8) | blue;

    public void SetUv(float u, float v)
    {
        _u = u;
        _v = v;
    }

    //The layout carries no overlay element
    public void SetUv1(int u, int v) { }

    //The packed light is reassembled from the two halves so the SetLight default still works
    public void SetUv2(int u, int v) => _light = ((v & 0xFFFF) << 16) | (u & 0xFFFF);

    public void SetNormal(float x, float y, float z)
    {
        _nx = x;
        _ny = y;
        _nz = z;
    }

    //The layout carries no line width element
    public void SetLineWidth(float width) { }

    //AddVertex3D is gone: PutBakedQuad now drives the consumer attribute by attribute, which keeps the interface
    //identical to vanilla instead of adding a convenience write only this class understood
    public static void PutBakedQuad(VertexConsumer consumer, PoseStack poseStack, in BakedQuad quad, QuadInstance instance)
    {
        var pose = poseStack.Copy();
        var normalMatrix = poseStack.Normal();
        var normalVec = quad.Direction.UnitVector();
        var normal = Vector3.Normalize(Vector3.TransformNormal(normalVec, normalMatrix));
        var lightEmission = quad.LightEmission;
        for (var i = 0; i < BakedQuad.VertexCount; i++)
        {
            var pos = Vector3.Transform(quad.Position(i), pose);
            var uv = quad.Uv(i);
            consumer.SetColor(instance.GetColor(i));
            consumer.SetUv(uv.X, uv.Y);
            consumer.SetLight(instance.GetLightCoordsWithEmission(i, lightEmission));
            consumer.SetNormal(normal.X, normal.Y, normal.Z);
            consumer.AddVertex(pos.X, pos.Y, pos.Z);
        }
    }

    public int VertexCount => Vertices.Count / 10;
    public void Clear() => Vertices.Clear();
}
