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

//CommonLayouts common layout factory, maps to vanilla net.minecraft.client.gui.layouts.CommonLayouts
//LabeledElement builds a vertical layout of label + element, label above and element below with spacing 4
public static class CommonLayouts
{
    private const int LabelSpacing = 4;

    //LabeledElement builds a vertical layout with a GuiLabel on top and the passed element below
    //maps to vanilla labeledElement omitting the Font param; on render GuiLabel gets it from the GlyphFont inside GuiRenderContext
    //label is a string rather than Component so the GPU layer does not depend on Network; rich text is handled by the domain layer
    //settings customizes the element cell's padding/align, maps to vanilla Consumer<LayoutSettings>
    public static ILayout LabeledElement(ILayoutElement element, string label, Action<LayoutSettings>? settings)
    {
        var layout = LinearLayout.Vertical().Spacing(LabelSpacing);
        layout.AddChild(new GuiLabel(label));
        var cellSettings = layout.NewCellSettings();
        settings?.Invoke(cellSettings);
        layout.AddChild(element, cellSettings);
        return layout;
    }

    //LabeledElement overload without settings, maps to vanilla labeledElement(font, element, label)
    public static ILayout LabeledElement(ILayoutElement element, string label)
        => LabeledElement(element, label, null);
}
