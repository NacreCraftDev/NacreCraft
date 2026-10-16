using System.Text;
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

//ChatScreen maps to vanilla ChatScreen
//Input box + message history; Enter sends but networking is not wired up yet, only local accumulation
public sealed class ChatScreen : Screen
{
    private GuiTextBox? _inputBox;
    private GuiLabel? _historyLabel;
    private readonly List<string> _history = new();

    public override string Title => I18n.Get("chat_screen.title");

    public override void Init()
    {
        var inputY = GuiHeight - 30;
        _inputBox = AddWidget(new GuiTextBox { X = 4, Y = inputY, Width = GuiWidth - 8, Height = 20 });
        _historyLabel = AddWidget(new GuiLabel { X = 4, Y = inputY - 100, Width = GuiWidth - 8, Height = 90, ForegroundColor = GuiColor.White });
        Window.FocusedControl = _inputBox;
    }

    public override void Tick()
    {
        if (_historyLabel is null || _history.Count == 0) return;
        var sb = new StringBuilder();
        foreach (var msg in _history.Skip(Math.Max(0, _history.Count - 5)))
            sb.AppendLine(msg);
        _historyLabel.Text = sb.ToString();
    }

    //Send adds the message to history and clears the input box; networking not wired up yet
    public void Send(string message)
    {
        _history.Add(message);
        if (_inputBox is not null) _inputBox.Text = string.Empty;
    }

    public override void OnClose() => Manager.SetScreen(new GameScreen());
    public override bool IsPauseScreen() => false;
}
