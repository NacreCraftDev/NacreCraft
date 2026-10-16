using RenderPipeline = NacreCraft.Client.Blaze3d.Pipeline.RenderPipeline;
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

namespace NacreCraft.Client.Gui.Render.State;

//GuiElementRenderState GUI element render state interface, maps to vanilla GuiElementRenderState
//RenderState subclasses implement this interface, submitting to GuiRenderState to participate in SortElements sorting and batching
//Immutable value object carrying pipeline/texture/pose snapshot/scissor snapshot
public interface GuiElementRenderState
{
    //BuildVertices builds vertices into the consumer
    void BuildVertices(VertexConsumer consumer);
    //Pipeline declarative render pipeline compiled by GuiRenderer via PipelineCache into a CompiledRenderPipeline
    RenderPipeline Pipeline { get; }
    //TextureSetup texture binding config
    TextureSetup TextureSetup { get; }
    //ScissorArea scissor rectangle
    ScreenRectangle ScissorArea { get; }
    //Bounds used for level intersection tests, computed from the element geometry+pose+scissor
    ScreenRectangle Bounds { get; }
}
