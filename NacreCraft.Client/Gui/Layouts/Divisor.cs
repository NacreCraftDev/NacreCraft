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
namespace NacreCraft.Client.Gui.Layouts;

//Divisor division helper, maps to vanilla com.mojang.math.Divisor
//Splits total into parts shares with the remainder going to the first few; each NextInt returns one share
//Used by GridLayout to divide the height/width of multi-row/column elements across rows and columns
public struct Divisor
{
    private readonly int _total;
    private readonly int _parts;
    private int _given;

    public Divisor(int total, int parts)
    {
        _total = total;
        _parts = parts;
        _given = 0;
    }

    //NextInt returns the next share; baseShare=total/parts with the remainder spread over the first remainder shares
    //The sum over all parts calls equals total
    public int NextInt()
    {
        if (_given >= _parts) return 0;
        int baseShare = _total / _parts;
        int remainder = _total % _parts;
        int result = baseShare + (_given < remainder ? 1 : 0);
        _given++;
        return result;
    }
}
