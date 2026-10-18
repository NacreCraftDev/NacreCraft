using System.IO;
using NacreCraft;
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

//PauseScreen pause menu, maps to vanilla PauseScreen
//Pops up on Esc during play, providing Resume/Options/Back to Main Menu buttons
//Background tiles the dirt texture, maps to vanilla options_background darken
public sealed class PauseScreen : Screen
{
    private GuiButton? _backToGameBtn;
    private GuiButton? _optionsBtn;
    private GuiButton? _quitToTitleBtn;
    //_dirtBg is not Added to Window; RenderBackground draws it directly to avoid duplication with the control tree
    private GuiImage? _dirtBg;

    public override string Title => I18n.Get("menu.game");

    //WantsBlur the pause menu blurs the dirt background, maps to vanilla PauseScreen's blur effect
    //The BeforeBlur pass draws dirt; after blurring, the AfterBlur pass layers sharp buttons on top
    public override bool WantsBlur => true;

    public override void Init()
    {
        var cx = GuiWidth / 2;
        var cy = GuiHeight / 2;
        //In-world menus use the inworld variant of the vanilla 32x32 menu background, which is already darkened
        var dirtId = Minecraft.GpuApp?.RegisterTexture(Identifier.WithDefaultNamespace("textures/gui/inworld_menu_background.png")) ?? 0;
        _dirtBg = new GuiImage
        {
            TextureId = dirtId,
            TextureWidth = 32,
            TextureHeight = 32,
            Tile = true,
            Tint = GuiColor.White,
            X = 0,
            Y = 0,
            Width = GuiWidth,
            Height = GuiHeight
        };
        //Title centered above the buttons
        AddWidget(new GuiLabel(I18n.Get("menu.game")) { X = cx - 100, Y = cy - 55, Width = 200, Height = 20 });
        var skin = RegisterButtonSprites();
        _backToGameBtn = AddWidget(new GuiButton(I18n.Get("menu.returnToGame")) { X = cx - 100, Y = cy - 30, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_backToGameBtn, skin);
        _backToGameBtn.Click += (_, _) => Manager.PopScreen();
        _optionsBtn = AddWidget(new GuiButton(I18n.Get("menu.options")) { X = cx - 100, Y = cy, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_optionsBtn, skin);
        _optionsBtn.Click += (_, _) => Manager.PushScreen(new OptionsScreen());
        _quitToTitleBtn = AddWidget(new GuiButton(I18n.Get("menu.returnToMenu")) { X = cx - 100, Y = cy + 30, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_quitToTitleBtn, skin);
        _quitToTitleBtn.Click += (_, _) => Manager.SetScreen(new TitleScreen());
    }

    //RenderBackground draws the tiled dirt background over the Window's solid color background
    //Before each redraw update Width/Height to match the ScaledWidth/Height after resize
    //At the end call BlurBeforeThisStratum so dirt goes into the BeforeBlur pass and subsequent controls into the AfterBlur pass
    public override void RenderBackground(IGuiRenderContext context)
    {
        if (_dirtBg is null) return;
        _dirtBg.Width = GuiWidth;
        _dirtBg.Height = GuiHeight;
        _dirtBg.Render(context);
        context.BlurBeforeThisStratum();
    }

    public override void OnClose() => Manager.PopScreen();
    public override bool IsPauseScreen() => true;
}
