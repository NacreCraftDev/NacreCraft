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

//HeaderAndFooterLayout three-section layout, maps to vanilla HeaderAndFooterLayout implements Layout
//header top, footer bottom, content centered with a top margin constrained not to cross the footer's top
//Vanilla depends on Screen; here screenWidth/screenHeight come from the constructor to remove the domain dependency and keep the GPU layer clean
//addTitleHeader depends on Font/Component and is omitted; the domain layer can addToHeader(new StringWidget) itself
public sealed class HeaderAndFooterLayout : ILayout
{
    public const int MagicPadding = 13;
    public const int DefaultHeaderAndFooterHeight = 33;
    private const int ContentMarginTop = 30;

    private readonly FrameLayout _headerFrame = new();
    private readonly FrameLayout _footerFrame = new();
    private readonly FrameLayout _contentsFrame = new();
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private int _headerHeight;
    private int _footerHeight;

    public HeaderAndFooterLayout(int screenWidth, int screenHeight)
        : this(screenWidth, screenHeight, DefaultHeaderAndFooterHeight) { }

    public HeaderAndFooterLayout(int screenWidth, int screenHeight, int headerAndFooterHeight)
        : this(screenWidth, screenHeight, headerAndFooterHeight, headerAndFooterHeight) { }

    public HeaderAndFooterLayout(int screenWidth, int screenHeight, int headerHeight, int footerHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _headerHeight = headerHeight;
        _footerHeight = footerHeight;
        //header/footer children are centered by default
        _headerFrame.DefaultChildLayoutSetting().Align(0.5f, 0.5f);
        _footerFrame.DefaultChildLayoutSetting().Align(0.5f, 0.5f);
    }

    //X/Y are no-ops; the layout is anchored to the screen origin and cannot move as a whole
    public int X { get => 0; set { } }
    public int Y { get => 0; set { } }
    public int Width => _screenWidth;
    public int Height => _screenHeight;

    public int HeaderHeight => _headerHeight;
    public int FooterHeight => _footerHeight;
    public void SetHeaderHeight(int height) => _headerHeight = height;
    public void SetFooterHeight(int height) => _footerHeight = height;

    //ContentHeight the screen height minus header/footer, the remaining usable height
    public int ContentHeight => _screenHeight - _headerHeight - _footerHeight;

    public void VisitChildren(Action<ILayoutElement> visitor)
    {
        _headerFrame.VisitChildren(visitor);
        _contentsFrame.VisitChildren(visitor);
        _footerFrame.VisitChildren(visitor);
    }

    public void RemoveChildren()
    {
        _headerFrame.RemoveChildren();
        _contentsFrame.RemoveChildren();
        _footerFrame.RemoveChildren();
    }

    //ArrangeElements header to the top, footer to the bottom, content centered and not crossing the footer's top
    public void ArrangeElements()
    {
        int headerHeight = _headerHeight;
        int footerHeight = _footerHeight;

        _headerFrame.SetMinWidth(_screenWidth);
        _headerFrame.SetMinHeight(headerHeight);
        _headerFrame.SetPosition(0, 0);
        _headerFrame.ArrangeElements();

        _footerFrame.SetMinWidth(_screenWidth);
        _footerFrame.SetMinHeight(footerHeight);
        _footerFrame.ArrangeElements();
        _footerFrame.Y = _screenHeight - footerHeight;

        _contentsFrame.SetMinWidth(_screenWidth);
        _contentsFrame.ArrangeElements();
        //content prefers Y=header+top margin but must not cross the footer's top
        int preferredContentY = headerHeight + ContentMarginTop;
        int maxContentY = _screenHeight - footerHeight - _contentsFrame.Height;
        _contentsFrame.SetPosition(0, Math.Min(preferredContentY, maxContentY));
    }

    public T AddToHeader<T>(T child) where T : ILayoutElement => _headerFrame.AddChild(child);
    public T AddToFooter<T>(T child) where T : ILayoutElement => _footerFrame.AddChild(child);
    public T AddToContents<T>(T child) where T : ILayoutElement => _contentsFrame.AddChild(child);

    public T AddToHeader<T>(T child, LayoutSettings settings) where T : ILayoutElement
        => _headerFrame.AddChild(child, settings);
    public T AddToFooter<T>(T child, LayoutSettings settings) where T : ILayoutElement
        => _footerFrame.AddChild(child, settings);
    public T AddToContents<T>(T child, LayoutSettings settings) where T : ILayoutElement
        => _contentsFrame.AddChild(child, settings);
}
