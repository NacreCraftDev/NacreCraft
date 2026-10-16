using NacreCraft.Client.Gui;
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
using NacreCraft.Client.Language;

namespace NacreCraft.Client.Gui.Screens;

//OptionsScreen options menu, maps to vanilla OptionsScreen
//HeaderAndFooterLayout main layout, maps to vanilla: contents with three buttons stacked vertically, footer with Done
public sealed class OptionsScreen : Screen
{
    private HeaderAndFooterLayout? _layout;

    public override string Title => I18n.Get("options.title");

    public override void Init()
    {
        var skin = RegisterButtonSprites();
        _layout = new HeaderAndFooterLayout(GuiWidth, GuiHeight);

        //contents three setting buttons stacked vertically; content is horizontally centered in contentsFrame
        var content = LinearLayout.Vertical().Spacing(10);
        content.AddChild(MakeNavButton(I18n.Get("options.video"), skin));
        content.AddChild(MakeNavButton(I18n.Get("options.sounds"), skin));
        content.AddChild(MakeNavButton(I18n.Get("options.controls"), skin));
        content.AddChild(MakeNavButton(I18n.Get("options.language"), skin, () => Manager.PushScreen(new LanguageScreen())));
        _layout.AddToContents(content, LayoutSettings.Defaults().Align(0.5f, 0f));

        //footer Done button; footer is centered by default
        _layout.AddToFooter(MakeNavButton(I18n.Get("gui.done"), skin, OnClose));

        //Recursively collect GuiControls in the layout, register them into Window, then arrange and position
        _layout.VisitChildren(elem => CollectWidgets(elem, c => AddWidget(c)));
        _layout.ArrangeElements();
    }

    //MakeNavButton creates a navigation button with unified width/height + skin + optional click
    private GuiButton MakeNavButton(string text, (string Normal, string Hover, string Disabled) skin, System.Action? onClick = null)
    {
        var btn = new GuiButton(text) { Width = 200, Height = 20 };
        ApplyButtonSpriteSkin(btn, skin);
        if (onClick is not null) btn.Click += (_, _) => onClick();
        return btn;
    }
}
