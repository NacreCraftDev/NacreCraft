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
namespace NacreCraft.Client.Render;

//ShaderDefines shader compile-time macro definitions, maps to vanilla ShaderDefines
//values is the key->string value macros; flags is the set of valueless flags
//The builder chains definitions, then Build returns an immutable snapshot
public sealed class ShaderDefines
{
    private readonly Dictionary<string, string> _values;
    private readonly HashSet<string> _flags;

    public IReadOnlyDictionary<string, string> Values => _values;
    public IReadOnlySet<string> Flags => _flags;

    private ShaderDefines(Dictionary<string, string> values, HashSet<string> flags)
    {
        _values = values;
        _flags = flags;
    }

    public static Builder NewBuilder() => new();

    public sealed class Builder
    {
        private readonly Dictionary<string, string> _values = new();
        private readonly HashSet<string> _flags = new();

        //Define valueless flag
        public Builder Define(string key)
        {
            _flags.Add(key);
            return this;
        }

        //Define integer-valued macro
        public Builder Define(string key, int value)
        {
            _values[key] = value.ToString();
            return this;
        }

        //Define float-valued macro
        public Builder Define(string key, float value)
        {
            _values[key] = value.ToString("R");
            return this;
        }

        //Define string-valued macro
        public Builder Define(string key, string value)
        {
            _values[key] = value;
            return this;
        }

        public ShaderDefines Build() => new(_values, _flags);
    }
}
