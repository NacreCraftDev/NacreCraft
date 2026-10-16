using System.Numerics;
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

namespace NacreCraft.Client.Render;

//IWorldRenderer world rendering abstraction interface
//LevelRenderer implements this in NacreCraft.Game, avoiding a circular dependency where VulkanGuiApp references NacreCraft.Game
//VulkanGuiApp holds an IWorldRenderer and calls Prepare/Upload/Draw in OnRecordCommandBuffer
//The ViewProj property lets VulkanGuiApp upload the ViewProj UBO to the shader
public interface IWorldRenderer
{
    //Prepare builds mesh data into the internal StagedVertexBuffer, rebuilt every frame; W8 changes it to async caching
    void Prepare();

    //Upload uploads vertices/indices to the GPU, reusing buffers across frames
    void Upload(GpuDevice device);

    //ViewProj the current frame's view*proj matrix; VulkanGuiApp reads this to upload set 0 UBO
    Matrix4x4 ViewProj { get; }

    //Perf metrics for GameScreen F3 to display world render stats
    int SectionCount { get; }
    int VisibleSectionCount { get; }
    int TotalVertexCount { get; }
    int DrawCallCount { get; }

    //Draw renders in Solid→Cutout→Translucent order; bindGlobals writes the view-projection matrix and atlas samplers after each SetPipeline
    void Draw(RenderPass pass, Action<RenderPass> bindGlobals);
}
