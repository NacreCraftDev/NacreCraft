using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Render;
using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Buffers;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Shaders;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Blaze3d.Vulkan;
using NacreCraft.Client.Blaze3d.Platform;
using NacreCraft.Client.Blaze3d.Font;
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

namespace NacreCraft.Client.Model;

//EntityModel entity model base class, maps to vanilla net.minecraft.client.model.EntityModel
//Holds the root ModelPart; subclasses build the model tree in the constructor and setupAnim drives animation
//renderToBuffer walks the ModelPart tree, generating vertices into an EntityVertexBuilder
//The Pipeline property specifies the render pipeline, defaulting to ENTITY_CUTOUT; subclasses may override
public abstract class EntityModel
{
    //Root model root node, filled in by subclasses at construction
    public ModelPart Root { get; }
    //Pipeline render pipeline defaulting to ENTITY_CUTOUT; subclasses may override to return SOLID/TRANSLUCENT
    public virtual RenderPipeline Pipeline => EntityRenderPipelines.ENTITY_CUTOUT;

    protected EntityModel(ModelPart root) => Root = root;

    //SetupAnim animation driver; subclasses override to adjust ModelPart rotations by state
    //PoC skeleton version with an empty implementation; W9.2 wires real animation
    public virtual void SetupAnim(EntityRenderState state) { }

    //RenderToBuffer renders the model tree into the builder
    //poseStack already holds the entity world transform (position+orientation); light/overlay/color come from EntityRenderer
    public void RenderToBuffer(PoseStack poseStack, EntityVertexBuilder builder, int lightCoords, int overlayCoords, int color)
    {
        Root.Render(poseStack, builder, lightCoords, overlayCoords, color);
    }
}
