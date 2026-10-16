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

//TitleScreen main menu, maps to vanilla TitleScreen
//The first screen on startup, providing Singleplayer/Multiplayer/Options/Quit buttons
//Background tiles the dirt texture, maps to vanilla options_background tiling dirt
public sealed class TitleScreen : Screen
{
    private GuiButton? _singlePlayerBtn;
    private GuiButton? _multiplayerBtn;
    private GuiButton? _optionsBtn;
    private GuiButton? _quitBtn;
    //_dirtBg is not Added to Window; RenderBackground draws it directly to avoid duplication with the control tree
    //Each Init calls RegisterTexture again to get a textureId, so it updates when the id changes after a swapchain rebuild
    private GuiImage? _dirtBg;

    public override string Title => I18n.Get("narrator.screen.title");

    public override void Init()
    {
        var cx = GuiWidth / 2;
        var cy = GuiHeight / 2;
        //If the dirt background tile texture fails to load, textureId=0 falls back to a gray placeholder without blocking the menu
        var dirtId = Minecraft.GpuApp?.RegisterTexture(Identifier.WithDefaultNamespace("textures/block/dirt.png")) ?? 0;
        _dirtBg = new GuiImage
        {
            TextureId = dirtId,
            TextureWidth = 16,
            TextureHeight = 16,
            Tile = true,
            //Vanilla TitleScreen overlays a dark tint on the dirt background to simulate darken
            Tint = GuiColor.FromRgb(64, 64, 64),
            X = 0,
            Y = 0,
            Width = GuiWidth,
            Height = GuiHeight
        };
        //Title and subtitle
        AddWidget(new GuiLabel("NacreCraft") { X = cx - 60, Y = cy - 130, Width = 160, Height = 20 });
        AddWidget(new GuiLabel("v0.1.0 Singleplayer Block World") { X = cx - 120, Y = cy - 105, Width = 240, Height = 16 });
        //Main button column with vanilla TitleScreen geometry: 200 wide, 20 high, 24 apart
        //The sprite is a nine-slice of 200x20 with a 3 pixel border, so matching the button to the source size also
        //keeps the middle segment a single tile and avoids a seam down the center
        var skin = RegisterButtonSprites();
        _singlePlayerBtn = AddWidget(new GuiButton(I18n.Get("menu.singleplayer")) { X = cx - 100, Y = cy - 55, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_singlePlayerBtn, skin);
        _singlePlayerBtn.Click += (_, _) => Manager.PushScreen(new GameScreen());
        _multiplayerBtn = AddWidget(new GuiButton(I18n.Get("menu.multiplayer")) { X = cx - 100, Y = cy - 31, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_multiplayerBtn, skin);
        //S3 connects to the local DedicatedServer, enters the world, switches to GameScreen; chunks are sent progressively via ChunkSender
        _multiplayerBtn.Click += (_, _) => Minecraft.ConnectServer("127.0.0.1", 25565);
        _optionsBtn = AddWidget(new GuiButton(I18n.Get("menu.options")) { X = cx - 100, Y = cy - 7, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_optionsBtn, skin);
        _optionsBtn.Click += (_, _) => Manager.PushScreen(new OptionsScreen());
        _quitBtn = AddWidget(new GuiButton(I18n.Get("menu.quit")) { X = cx - 100, Y = cy + 17, Width = 200, Height = 20 });
        ApplyButtonSpriteSkin(_quitBtn, skin);
        _quitBtn.Click += (_, _) => Minecraft.Stop();
        //Bottom status line
        AddWidget(new GuiLabel("© 2026 NacreCraft | Press F3 for debug info") { X = cx - 150, Y = GuiHeight - 18, Width = 300, Height = 14 });
    }

    //RenderBackground draws the tiled dirt background over the Window's solid color background
    //Before each redraw update Width/Height to match the ScaledWidth/Height after resize
    public override void RenderBackground(IGuiRenderContext context)
    {
        if (_dirtBg is null) return;
        _dirtBg.Width = GuiWidth;
        _dirtBg.Height = GuiHeight;
        _dirtBg.Render(context);
    }

    public override void OnClose() => Minecraft.Stop();
    public override bool IsPauseScreen() => true;
}
