using System.Numerics;
using NacreCraft.Client.Blaze3d.Pipeline;
using NacreCraft.Client.Blaze3d.Textures;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Rendertype;

namespace NacreCraft.Client.Gui.Font;

//TextRenderable one drawable text run, maps to vanilla net.minecraft.client.gui.font.TextRenderable
public interface TextRenderable
{
    void Render(Matrix4x4 pose, VertexConsumer buffer, int packedLightCoords, bool flat);

    RenderType RenderType(DisplayMode displayMode);

    GpuTextureView TextureView();

    RenderPipeline GuiPipeline();

    float Left();

    float Top();

    float Right();

    float Bottom();

    //Styled marks a renderable that also exposes its clickable area
    public interface Styled : TextRenderable, ActiveArea
    {
        float ActiveArea.ActiveLeft => Left();

        float ActiveArea.ActiveTop => Top();

        float ActiveArea.ActiveRight => Right();

        float ActiveArea.ActiveBottom => Bottom();
    }
}
