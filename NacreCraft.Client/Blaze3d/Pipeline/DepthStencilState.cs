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
namespace NacreCraft.Client.Blaze3d.Pipeline;

//DepthStencilState depth-stencil state, maps to vanilla DepthStencilState record
//depthTest=null means depth testing is disabled; writeDepth controls whether the depth buffer is written
//depthBiasScaleFactor/depthBiasConstant used to eliminate z-fighting
public readonly record struct DepthStencilState(
    CompareOp? DepthTest,
    bool WriteDepth,
    float DepthBiasScaleFactor,
    float DepthBiasConstant)
{
    //DEFAULT default depth state Less write no bias
    //The current Camera projection uses the standard Vulkan [0,1] depth near=0 far=1; Less matches clearDepth=1
    //Vanilla uses GreaterOrEqual because of reversed-Z; NacreCraft does not use reversed-Z and switches to Less for consistency
    public static readonly DepthStencilState DEFAULT = new(CompareOp.Less, true, 0f, 0f);

    public DepthStencilState(CompareOp depthTest, bool depthWrite)
        : this(depthTest, depthWrite, 0f, 0f) { }
}
