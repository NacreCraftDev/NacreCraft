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
namespace NacreCraft.Client.Gui.Font;

//FontOption font option, maps to vanilla FontOption
//The uniform/alt/illageralt filters match providers against the active options
public sealed class FontOption
{
    public string Name { get; }
    public static readonly FontOption Uniform = new("uniform");
    public static readonly FontOption Alt = new("alt");
    public static readonly FontOption IllagerAlt = new("illageralt");

    private FontOption(string name) { Name = name; }

    public override string ToString() => Name;
    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is FontOption o && o.Name == Name;
}

//FontOptionFilter filter condition, maps to vanilla FontOption.Filter
//Holds the FontOption→required mapping; Apply checks whether the options set satisfies all conditions
//The filter:{uniform:false} in default.json matches when the uniform option is not active
public sealed class FontOptionFilter
{
    private readonly Dictionary<FontOption, bool> _conditions;
    public static readonly FontOptionFilter AlwaysPass = new(new Dictionary<FontOption, bool>());

    public FontOptionFilter(Dictionary<FontOption, bool> conditions) => _conditions = conditions;

    //Apply returns true when the options set satisfies all conditions; an empty condition always passes
    public bool Apply(IReadOnlySet<FontOption> options)
    {
        foreach (var (option, required) in _conditions)
            if (options.Contains(option) != required) return false;
        return true;
    }
}
