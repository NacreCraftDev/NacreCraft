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

namespace NacreCraft.Client.Render.Entity.State;

//EntityRenderState entity render state, maps to vanilla EntityRenderState
//Holds the position/orientation/animation/lighting data needed for entity rendering, extracted from Entity by EntityRenderer
//W9.2 extends the animation inputs AgeInTicks/WalkAnimation and LightCoords real lighting
public sealed class EntityRenderState
{
    //Position entity world position
    public Vector3 Position { get; set; }
    //YRot entity Y-axis rotation in radians, the heading
    public float YRot { get; set; }
    //XRot entity X-axis rotation in radians, the pitch
    public float XRot { get; set; }
    //YBodyRot body heading in radians, separate from YRot so the head can turn independently of the body
    public float YBodyRot { get; set; }
    //AgeInTicks entity's tick count driving idle animation, maps to vanilla ageInTicks
    public float AgeInTicks { get; set; }
    //BobOffset bobbing phase, maps to vanilla ItemEntity's bobOffs derived from the entity id for stability
    public float BobOffset { get; set; }
    //WalkAnimationPos walk animation phase (limbSwing), accumulated from movement distance, maps to vanilla walkAnimationPos
    public float WalkAnimationPos { get; set; }
    //WalkAnimationSpeed walk animation speed (limbSwingAmount) 0-1 movement intensity, maps to vanilla walkAnimationSpeed
    public float WalkAnimationSpeed { get; set; }
    //LightCoords packed int light coordinates, filled by the caller from the light level at the entity's position
    //Defaults to full bright for callers that have not wired lighting
    public int LightCoords { get; set; } = LightTexture.FullBrightCoords;
    //Name entity name, for debugging
    public string Name { get; set; } = string.Empty;
    //CustomData renderer-specific data interpreted by the concrete EntityRenderer
    //The GPU layer does not know domain types; a dropped item's ItemStack is passed through here to ItemEntityRenderer
    public object? CustomData { get; set; }
}
