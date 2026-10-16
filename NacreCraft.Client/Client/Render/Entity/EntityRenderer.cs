using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Resource;
using NacreCraft.Client.Blaze3d.Audio;
using NacreCraft.Client.Blaze3d.Framegraph;
using NacreCraft.Client.Blaze3d.Preprocessor;
using NacreCraft.Client.Blaze3d.Util;
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

namespace NacreCraft.Client.Render.Entity;

//EntityRenderer entity renderer base class, maps to vanilla net.minecraft.client.renderer.entity.EntityRenderer
//Subclasses hold an EntityModel and implement Render to generate vertices; one Renderer instance per entity type
//shadowRadius/shadowStrength shadow parameters; the PoC skeleton does not render shadows, W9.2 adds them
public abstract class EntityRenderer
{
    //Model entity model, provided by subclasses
    public abstract EntityModel Model { get; }

    //Pipeline render pipeline defaults to Model.Pipeline; subclasses may override
    public virtual RenderPipeline Pipeline => Model.Pipeline;

    //ShadowRadius shadow radius; 0 means no shadow
    public virtual float ShadowRadius => 0f;

    //ShadowStrength shadow strength
    public virtual float ShadowStrength => 1f;

    //Render generates entity vertices and writes them into builder
    //poseStack already contains all transforms except the camera projection; the caller pushes the entity world transform
    //state entity render state, including position, orientation, animation, and light
    //overlayCoords overlay coordinates, default 0, no hurt red flash
    //color color multiplier, default -1 (white, unmodulated), maps to vanilla
    public virtual void Render(PoseStack poseStack, EntityVertexBuilder builder, EntityRenderState state)
    {
        Model.SetupAnim(state);
        //Light coordinates are read from state and filled by the caller from the light level at the entity's position; FullBright by default
        Model.RenderToBuffer(poseStack, builder, state.LightCoords, 0, -1);
    }
}
