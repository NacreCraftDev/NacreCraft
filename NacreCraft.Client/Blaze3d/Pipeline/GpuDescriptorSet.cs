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

//GpuDescriptorType descriptor type
public enum GpuDescriptorType
{
    UniformBuffer,
    CombinedImageSampler
}

//GpuShaderStageFlags shader stage visibility bitmask
[Flags]
public enum GpuShaderStageFlags
{
    None = 0,
    Vertex = 1,
    Fragment = 2,
    Compute = 4,
    AllGraphics = Vertex | Fragment
}

//GpuDescriptorBinding description of a single descriptor binding
public sealed class GpuDescriptorBinding
{
    public int Binding { get; set; }
    public GpuDescriptorType DescriptorType { get; set; }
    public GpuShaderStageFlags StageFlags { get; set; } = GpuShaderStageFlags.AllGraphics;
    public int DescriptorCount { get; set; } = 1;
}

//GpuDescriptorLayoutDescription descriptor set layout description
public sealed class GpuDescriptorLayoutDescription
{
    public List<GpuDescriptorBinding> Bindings { get; set; } = new();
}

//GpuDescriptorLayout descriptor set layout abstraction
//Subclasses create the underlying layout object (Vulkan VkDescriptorSetLayout)
public abstract class GpuDescriptorLayout : IDisposable
{
    public GpuDescriptorLayoutDescription Description { get; }

    protected GpuDescriptorLayout(GpuDescriptorLayoutDescription description)
    {
        Description = description;
    }

    public virtual void Dispose() { }
}

//GpuDescriptorSet descriptor set instance
//WriteBuffer binds a uniform buffer to the given binding slot
//WriteImage binds a combined image sampler to the given binding slot
public abstract class GpuDescriptorSet : IDisposable
{
    public GpuDescriptorLayout Layout { get; }

    protected GpuDescriptorSet(GpuDescriptorLayout layout)
    {
        Layout = layout;
    }

    //WriteBuffer binds a uniform buffer; range=-1 means the whole buffer
    public abstract void WriteBuffer(int binding, GpuBuffer buffer, int offset = 0, int range = -1);

    //WriteImage binds a combined image sampler
    public abstract void WriteImage(int binding, GpuTexture texture, GpuSampler sampler);

    public virtual void Dispose() { }
}
