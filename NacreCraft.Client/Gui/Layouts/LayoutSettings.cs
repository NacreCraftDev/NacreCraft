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

//LayoutSettings layout params interface, maps to vanilla LayoutSettings
//padding per side, align a continuous 0~1 value, chained API
//0=left/top 0.5=center 1=right/bottom
//Java allows a field and method with the same name, C# does not; the Impl fields take a Value suffix to avoid clashing with the method names while keeping parity with vanilla
public interface LayoutSettings
{
    LayoutSettings Padding(int padding);
    LayoutSettings Padding(int horizontal, int vertical);
    LayoutSettings Padding(int left, int top, int right, int bottom);
    LayoutSettings PaddingLeft(int padding);
    LayoutSettings PaddingTop(int padding);
    LayoutSettings PaddingRight(int padding);
    LayoutSettings PaddingBottom(int padding);
    LayoutSettings PaddingHorizontal(int padding);
    LayoutSettings PaddingVertical(int padding);
    LayoutSettings Align(float xAlignment, float yAlignment);
    LayoutSettings AlignHorizontally(float xAlignment);
    LayoutSettings AlignVertically(float yAlignment);
    LayoutSettings Copy();

    //GetExposed exposes the mutable inner Impl so the layout engine reads/writes the fields directly
    Impl GetExposed();

    //Defaults default config padding=0 align=0,0 top-left aligned
    static LayoutSettings Defaults() => new Impl();

    //Impl mutable implementation; the layout engine reads/writes this class's fields directly
    //maps to vanilla LayoutSettingsImpl paddingLeft/Top/Right/Bottom + xAlignment/yAlignment
    public sealed class Impl : LayoutSettings
    {
        public int PaddingLeftValue;
        public int PaddingTopValue;
        public int PaddingRightValue;
        public int PaddingBottomValue;
        public float XAlignment;
        public float YAlignment;

        public Impl() { }

        public Impl(Impl copy)
        {
            PaddingLeftValue = copy.PaddingLeftValue;
            PaddingTopValue = copy.PaddingTopValue;
            PaddingRightValue = copy.PaddingRightValue;
            PaddingBottomValue = copy.PaddingBottomValue;
            XAlignment = copy.XAlignment;
            YAlignment = copy.YAlignment;
        }

        public LayoutSettings Padding(int padding) => Padding(padding, padding);
        public LayoutSettings Padding(int horizontal, int vertical)
            => PaddingHorizontal(horizontal).PaddingVertical(vertical);
        public LayoutSettings Padding(int left, int top, int right, int bottom)
            => PaddingLeft(left).PaddingRight(right).PaddingTop(top).PaddingBottom(bottom);

        public LayoutSettings PaddingLeft(int padding) { PaddingLeftValue = padding; return this; }
        public LayoutSettings PaddingTop(int padding) { PaddingTopValue = padding; return this; }
        public LayoutSettings PaddingRight(int padding) { PaddingRightValue = padding; return this; }
        public LayoutSettings PaddingBottom(int padding) { PaddingBottomValue = padding; return this; }
        public LayoutSettings PaddingHorizontal(int padding)
            => PaddingLeft(padding).PaddingRight(padding);
        public LayoutSettings PaddingVertical(int padding)
            => PaddingTop(padding).PaddingBottom(padding);

        public LayoutSettings Align(float xAlignment, float yAlignment)
        { XAlignment = xAlignment; YAlignment = yAlignment; return this; }
        public LayoutSettings AlignHorizontally(float xAlignment)
        { XAlignment = xAlignment; return this; }
        public LayoutSettings AlignVertically(float yAlignment)
        { YAlignment = yAlignment; return this; }

        public LayoutSettings Copy() => new Impl(this);
        public Impl GetExposed() => this;
    }
}
