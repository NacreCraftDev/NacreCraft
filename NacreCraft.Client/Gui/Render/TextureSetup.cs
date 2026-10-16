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
namespace NacreCraft.Client.Gui.Render;

//TextureSetup texture binding config, maps to vanilla TextureSetup
//At most 3 texture+sampler pairs participating in batching and sorting
//Equals reference equality used for SortElements batching decisions
public sealed class TextureSetup
{
    public GpuTexture? Texture0 { get; }
    public GpuSampler? Sampler0 { get; }
    public GpuTexture? Texture1 { get; }
    public GpuSampler? Sampler1 { get; }
    public GpuTexture? Texture2 { get; }
    public GpuSampler? Sampler2 { get; }

    private TextureSetup(GpuTexture? t0, GpuSampler? s0, GpuTexture? t1, GpuSampler? s1, GpuTexture? t2, GpuSampler? s2)
    {
        Texture0 = t0; Sampler0 = s0;
        Texture1 = t1; Sampler1 = s1;
        Texture2 = t2; Sampler2 = s2;
    }

    //NoTexture shared singleton for solid-color pipelines with no texture
    public static readonly TextureSetup NoTexture = new(null, null, null, null, null, null);

    //SingleTexture single texture binding
    public static TextureSetup SingleTexture(GpuTexture texture, GpuSampler sampler)
        => new(texture, sampler, null, null, null, null);

    //SingleTextureWithLightmap single texture + lightmap two textures
    public static TextureSetup SingleTextureWithLightmap(GpuTexture texture, GpuSampler sampler, GpuTexture lightmap, GpuSampler lightmapSampler)
        => new(texture, sampler, lightmap, lightmapSampler, null, null);

    public override bool Equals(object? obj)
    {
        if (obj is not TextureSetup other) return false;
        return ReferenceEquals(Texture0, other.Texture0) && ReferenceEquals(Sampler0, other.Sampler0)
            && ReferenceEquals(Texture1, other.Texture1) && ReferenceEquals(Sampler1, other.Sampler1)
            && ReferenceEquals(Texture2, other.Texture2) && ReferenceEquals(Sampler2, other.Sampler2);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Texture0); hash.Add(Sampler0);
        hash.Add(Texture1); hash.Add(Sampler1);
        hash.Add(Texture2); hash.Add(Sampler2);
        return hash.ToHashCode();
    }
}
