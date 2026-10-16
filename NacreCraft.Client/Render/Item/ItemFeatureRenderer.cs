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

//ItemFeatureRenderer item feature renderer, maps to vanilla ItemFeatureRenderer
//Execute walks the ItemSubmitCollector nodes, rebuilding the pose with a temporary PoseStack then putBakedQuad into the VertexConsumer
//Vanilla splits into prepare/execute stages + phase groups; the PoC simplifies to one stage writing directly
//FULL_BRIGHT full-bright light coordinates GUI items use NO_OVERLAY, no overlay
public static class ItemFeatureRenderer
{
    //Full-bright light coordinates blocklight=15 skylight=15 packed
    public const int FullBright = 0x00F000F0;
    //No overlay
    public const int NoOverlay = 0;

    //Execute renders all the collector's submit nodes to the IVertexConsumer
    //Each node rebuilds a temporary PoseStack from its pose snapshot, then PutBakedQuad
    //The tint color is looked up by quad.TintIndex via ItemTints.GetTint and applied to the vertex color field
    public static void Execute(ItemSubmitCollector collector, VertexConsumer consumer)
    {
        var tempPose = new PoseStack();
        var instance = new QuadInstance();
        foreach (var node in collector.Nodes)
        {
            tempPose.SetIdentity();
            tempPose.MulPose(node.Pose);
            instance.LightCoords = node.LightCoords;
            instance.OverlayCoords = node.OverlayCoords;
            foreach (var quad in node.Quads)
            {
                instance.Color = ItemTints.GetTint(quad.TintIndex);
                VertexConsumer3D.PutBakedQuad(consumer, tempPose, in quad, instance);
            }
        }
    }
}
